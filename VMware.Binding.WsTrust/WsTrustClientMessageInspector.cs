/*
 ********************************************************
 * Copyright (c) VMware, Inc. 2016. All Rights Reserved.
 ********************************************************
 *
 * DISCLAIMER. THIS PROGRAM IS PROVIDED TO YOU "AS IS" WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, WHETHER ORAL OR WRITTEN,
 * EXPRESS OR IMPLIED. THE AUTHOR SPECIFICALLY DISCLAIMS ANY IMPLIED
 * WARRANTIES OR CONDITIONS OF MERCHANTABILITY, SATISFACTORY QUALITY,
 * NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.
 */

using System;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Security;
using System.Xml;
using System.Xml.Serialization;
using VMware.Binding.WsTrust.Types;

namespace VMware.Binding.WsTrust
{
    /// <summary>
    ///    Message interceptor that modifies the message to include the
    ///    WS-Security protocol headers.
    /// </summary>
    public class WsTrustClientMessageInspector : IClientMessageInspector
    {
        private readonly XmlElement _samlToken;
        private const string SOAP_ENVELOPE_NS = "http://schemas.xmlsoap.org/soap/envelope/";

        private const string WS_SECURITY_NS =
           "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        private const string WS_UTILITY_NS =
           "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        private const string SOAP_HEADER_ELEMENT_NAME = "Header";
        private const string SECURITY_TOKEN_ID_PREFIX = "SecurityToken";
        private const string TIMESTAMP_ID_PREFIX = "Timestamp";
        private const string BODY_ID_PREFIX = "Id";

        /// <summary>
        ///    Headers are buffered.
        ///    The maximum size in bytes of a header.
        /// </summary>
        private const int MAX_SIZE_OF_HEADERS = 1 * 1024 * 1024; //1MB should be enough to hold a token.

        private const string SOAP_ENVELOPE_ELEMENT_NAME = "Envelope";

        /// <summary>
        ///    The time span a request is considered valid.
        /// </summary>
        private static readonly TimeSpan RequestTimestampValidPeriod = new TimeSpan(0, 0, 5, 0);

        private static readonly XmlSerializer SecurityHeaderSerializer =
           new XmlSerializer(typeof(SecurityHeader), WS_SECURITY_NS);

        private readonly WSSecurityTokenSerializer _tokenSerializer =
           new WSSecurityTokenSerializer(SecurityVersion.WSSecurity10, true);

        private readonly X509SecurityToken _certificateToken;
        private readonly UserNameSecurityToken _userNameToken;

        public WsTrustClientMessageInspector(ClientCredentials clientCredentials)
        {
            // Reusing the WCF tokens as there is a built-in serializer for those.
            if (clientCredentials.ClientCertificate.Certificate != null)
            {
                string tokenId = CreateElementId(SECURITY_TOKEN_ID_PREFIX);

                _certificateToken = new X509SecurityToken(
                   clientCredentials.ClientCertificate.Certificate,
                   tokenId);
            }

            if (clientCredentials.UserName.UserName != null)
            {
                string tokenId = CreateElementId(SECURITY_TOKEN_ID_PREFIX);
                _userNameToken = new UserNameSecurityToken(
                   clientCredentials.UserName.UserName,
                   clientCredentials.UserName.Password,
                   tokenId);
            }
        }

        public WsTrustClientMessageInspector(ClientCredentials clientCredentials, XmlElement samlToken)
            : this(clientCredentials)
        {
            _samlToken = samlToken;
        }

        private static string CreateElementId(string prefix)
        {
            return string.Format("{0}-{1:D}", prefix, Guid.NewGuid());
        }

        #region IClientMessageInspector

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            // nothing needs to be done on reply.
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            // Working with the message in XML form
            XmlDocument soapRequest = GetXmlMessage(request);

            // Setting an ID to the body to be able to reference it in the signature (to be able to sign it)
            string bodyId = CreateElementId(BODY_ID_PREFIX);
            SetIdToBodyElement(soapRequest.DocumentElement, bodyId);

            SecurityHeader wsSecurityHeader = GetUnsignedHeader();

            // Converting the sso header from object to xml to easily assign xml values to properties.
            XmlDocument securityHeaderXml = ToXmlDocument(wsSecurityHeader);

            // Attaching the header without the signature
            var ssoHeaderElement = MergeMessageWithHeader(
               soapRequest.DocumentElement,
               securityHeaderXml.DocumentElement);

            if (_certificateToken != null)
            {
                // Get a key identifier to the key depending on how the key is specified.
                XmlElement keyIdentifier = _samlToken != null
                   ? GetKeyIdentifierClause(_samlToken)
                   : GetKeyIdentifierClause(_certificateToken);

                // Compute the signature on the timestamp and body elements.
                var signature = Util.ComputeSignature(
                   soapRequest,
                   keyIdentifier,
                   _certificateToken.Certificate.PrivateKey,
                   bodyId,
                   wsSecurityHeader.Timestamp.Id);

                // Inserting the signature into the security header (and the message respectively).
                ssoHeaderElement.AppendChild(signature);
            }

            // Convert the SOAP request back to a message that replaces the original message
            request = ToMessage(soapRequest, request.Version, request.Headers.Action, request.Properties);

            // No need to correlate requests with replays for this inspector.
            return null;
        }

        #endregion IClientMessageInspector

        private XmlElement ToXmlElement(SecurityToken token)
        {
            using (XmlDocumentWriterHelper documentWriterHelper = new XmlDocumentWriterHelper())
            {
                _tokenSerializer.WriteToken(documentWriterHelper.CreateDocumentWriter(), token);
                XmlDocument xmlDocument = documentWriterHelper.ReadDocument();

                return xmlDocument.DocumentElement;
            }
        }

        private Message ToMessage(
           XmlDocument xmlRequest,
           MessageVersion version,
           string action,
           MessageProperties messageProperties)
        {
            Debug.Assert(xmlRequest.DocumentElement != null, "xmlRequest.DocumentElement != null");

            XmlNodeReader xmlReader = new XmlNodeReader(xmlRequest.DocumentElement);

            var message = Message.CreateMessage(xmlReader, MAX_SIZE_OF_HEADERS, version);
            message.Headers.Action = action;
            message.Properties.CopyProperties(messageProperties);
            return message;
        }

        private static XmlDocument GetXmlMessage(Message request)
        {
            using (XmlDocumentWriterHelper documentWriterHelper = new XmlDocumentWriterHelper())
            {
                request.WriteMessage(documentWriterHelper.CreateDocumentWriter());
                return documentWriterHelper.ReadDocument();
            }
        }

        private static XmlElement MergeMessageWithHeader(
           XmlElement xmlRequest,
           XmlElement ssoHeaderXml)
        {
            Debug.Assert(xmlRequest.OwnerDocument != null);

            var headersNode = GetOrCreateHeadersNode(xmlRequest);

            var ssoHeaderNode = xmlRequest.OwnerDocument.ImportNode(ssoHeaderXml, true);

            return (XmlElement)headersNode.AppendChild(ssoHeaderNode);
        }

        private static XmlElement GetOrCreateHeadersNode(XmlElement envelopeElement)
        {
            Debug.Assert(envelopeElement != null);
            Debug.Assert(
               envelopeElement.LocalName == SOAP_ENVELOPE_ELEMENT_NAME
               && envelopeElement.NamespaceURI == SOAP_ENVELOPE_NS,
               "Expected an Envelope element.");

            XmlNodeList headerElements =
               envelopeElement.GetElementsByTagName(SOAP_HEADER_ELEMENT_NAME, SOAP_ENVELOPE_NS);

            Debug.Assert(headerElements.Count <= 1, "Found multiple Header elements in the SOAP envelope.");

            XmlElement headerElement;
            if (headerElements.Count == 0)
            {
                XmlDocument ownerDocument = envelopeElement.OwnerDocument;
                Debug.Assert(ownerDocument != null);

                string soapPrefix = envelopeElement.GetPrefixOfNamespace(SOAP_ENVELOPE_NS);
                Debug.Assert(soapPrefix != null);

                headerElement = ownerDocument.CreateElement(soapPrefix, SOAP_HEADER_ELEMENT_NAME, SOAP_ENVELOPE_NS);
                envelopeElement.AppendChild(headerElement);
            }
            else
            {
                headerElement = (XmlElement)headerElements[0];
            }

            return headerElement;
        }

        private static XmlDocument ToXmlDocument(SecurityHeader value)
        {
            using (XmlDocumentWriterHelper documentWriterHelper = new XmlDocumentWriterHelper())
            {
                SecurityHeaderSerializer.Serialize(documentWriterHelper.CreateDocumentWriter(), value);

                return documentWriterHelper.ReadDocument();
            }
        }

        private SecurityHeader GetUnsignedHeader()
        {
            // The header should have a mustUnderstand attribute value of 1
            var mustUnderstandAttribute =
               Util.CreateXmlAttribute("mustUnderstand", SOAP_ENVELOPE_NS, "1");

            DateTime createdTime = DateTime.Now;
            DateTime expiresTime = createdTime + RequestTimestampValidPeriod;

            string timestampId = CreateElementId(TIMESTAMP_ID_PREFIX);

            SecurityHeader ssoHeader = new SecurityHeader
            {
                Timestamp = new Timestamp
                {
                    Id = timestampId,
                    Created =
                       new AttributedDateTime
                       {
                           Value = createdTime
                       },
                    Expires =
                       new AttributedDateTime
                       {
                           Value = expiresTime
                       }
                },
                AnyAttr = new[] { mustUnderstandAttribute }
            };

            if (_userNameToken != null)
            {
                ssoHeader.UsernameToken = ToXmlElement(_userNameToken);
            }

            if (_samlToken != null)
            {
                ssoHeader.SamlToken = _samlToken;
            }
            else
            {
                if (_certificateToken != null)
                {
                    // The X509 token is serialized as a binary security token in the security header.
                    var binarySecurityToken = ToXmlElement(_certificateToken);
                    ssoHeader.BinarySecurityToken = binarySecurityToken;
                }
            }

            return ssoHeader;
        }

        private static void SetIdToBodyElement(XmlElement envelopeElement, string bodyId)
        {
            Debug.Assert(envelopeElement != null);
            Debug.Assert(
               envelopeElement.LocalName == SOAP_ENVELOPE_ELEMENT_NAME
               && envelopeElement.NamespaceURI == SOAP_ENVELOPE_NS,
               "Expected an Envelope element.");

            XmlNodeList elements = envelopeElement.GetElementsByTagName("Body", SOAP_ENVELOPE_NS);
            Debug.Assert(elements.Count == 1, "Expected one Body element in SOAP envelope.");

            var bodyElement = (XmlElement)elements[0];
            SetIdToElement(bodyElement, bodyId, WS_UTILITY_NS);
        }

        private static void SetIdToElement(
           XmlElement element,
           string value,
           string namespaceUri)
        {
            const string attributeName = "Id";

            XmlAttribute attributeNode = element.GetAttributeNode(attributeName, namespaceUri);

            if (attributeNode == null)
            {
                // Note: Creating an attribute without a prefix would cause underterministic
                // serialization of the XmlDocument to text and cause issues with signature
                // validity.
                attributeNode =
                   Util.CreateXmlAttribute(element, "wsu", attributeName, namespaceUri, value);
            }

            attributeNode.Value = value;
        }

        private XmlElement GetKeyIdentifierClause(X509SecurityToken certToken)
        {
            using (XmlDocumentWriterHelper documentWriterHelper = new XmlDocumentWriterHelper())
            {
                // The key is located in the same XML document, so referencing that
                // with a local key identifier.
                var keyIdentifierClause =
                   certToken.CreateKeyIdentifierClause<LocalIdKeyIdentifierClause>();
                Debug.Assert(keyIdentifierClause != null);

                _tokenSerializer.WriteKeyIdentifierClause(
                   documentWriterHelper.CreateDocumentWriter(), keyIdentifierClause);

                XmlDocument xmlDocument = documentWriterHelper.ReadDocument();

                return xmlDocument.DocumentElement;
            }
        }

        private XmlElement GetKeyIdentifierClause(XmlElement samlToken)
        {
            if (samlToken == null) throw new ArgumentNullException("samlToken");

            using (XmlDocumentWriterHelper documentWriterHelper = new XmlDocumentWriterHelper())
            {
                // The key is located in the SAML token. The identifier is to the SAML token.

                XmlNode idAttribute = samlToken.Attributes.GetNamedItem("ID");

                if (idAttribute == null)
                {
                    throw new InvalidDataException("The SAML token does not have an ID attribute.");
                }

                string samlTokenId = idAttribute.Value;

                SecurityKeyIdentifierClause keyIdentifierClause = new Saml2AssertionKeyIdentifierClause(samlTokenId);

                Saml2SecurityTokenHandler handler = new Saml2SecurityTokenHandler();
                handler.KeyInfoSerializer.WriteKeyIdentifierClause(documentWriterHelper.CreateDocumentWriter(),
                   keyIdentifierClause);

                XmlDocument xmlDocument = documentWriterHelper.ReadDocument();

                return xmlDocument.DocumentElement;
            }
        }
    }
}