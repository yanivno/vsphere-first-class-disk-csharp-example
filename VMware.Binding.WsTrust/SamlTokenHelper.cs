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



namespace VMware.Binding.WsTrust

{

    using System;

    using System.Collections.Generic;

    using System.Configuration;

    using System.Net;

    using System.Net.Security;

    using System.Security.Cryptography.X509Certificates;

    using System.ServiceModel;

    using System.ServiceModel.Channels;

    using System.ServiceModel.Description;

    using System.Text;

    using System.Xml;

    using Vim25Api;

    using vmware.sso;



    public class SamlTokenHelper

    {

        # region variable declaration

        public static string strDateFormat = "{0:yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'}";

        public static string strAssertionId = "ID";

        public static string strIssueInstant = "IssueInstant";

        public static string strSubjectConfirmationNode = "saml2:SubjectConfirmation";

        public static string strSubjectConfirmationMethodValueAttribute = "Method";

        public static string strSubjectConfirmationMethodValueTypeBearer = "urn:oasis:names:tc:SAML:2.0:cm:bearer";

        public static string strSubjectConfirmationMethodValueTypeHoK = "urn:oasis:names:tc:SAML:2.0:cm:holder-of-key";

        private static long maxReceivedMessageSize = Int32.MaxValue;

        # endregion



        # region private function definition



        private static MessageEncodingBindingElement GetWcfEncoding()

        {

            // VMware STS requires SOAP version 1.1

            return new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8);

        }



        private static HttpsTransportBindingElement GetWcfTransport()

        {

            // Communication with the STS is over https

            HttpsTransportBindingElement transport = new HttpsTransportBindingElement

            {

                RequireClientCertificate = false,

                AllowCookies = true,

                MaxReceivedMessageSize = maxReceivedMessageSize

            };



            return transport;

        }



        # endregion



        # region public function definition



        public static CustomBinding GetWcfBinding()

        {

            // There is no build-in WCF binding capable of communicating

            // with VMware STS, so we create a custom one.

            // The binding does not provide support for WS-Trust,

            // that is currently implemented as a WCF endpoint behaviour.

            return new CustomBinding(

               GetWcfEncoding(),

               GetWcfTransport());

        }



        public static void SetCredentials(

         string username,

         string password,

         X509Certificate2 signingCertificate,

         ClientCredentials clientCredentials)

        {

            if (clientCredentials == null) throw new ArgumentNullException("clientCredentials");



            clientCredentials.UserName.UserName = username;

            clientCredentials.UserName.Password = password;

            clientCredentials.ClientCertificate.Certificate = signingCertificate;

            clientCredentials.SupportInteractive = false;

        }



        public static void SetupServerCertificateValidation()

        {

            // validates or ignores server certificate errors based on the

            // value of config key "ValidateServerCertificate"

            var validateServerCert = ConfigurationManager.AppSettings[

                "ValidateServerCertificate"];



            if (!string.IsNullOrWhiteSpace(validateServerCert) &&

                validateServerCert.Equals(bool.TrueString,

                StringComparison.CurrentCultureIgnoreCase))

            {

                // validates server certificate

                ServicePointManager.ServerCertificateValidationCallback +=

                    Validate;

            }

            else

            {

                // ignores server certificate errors

                ServicePointManager.ServerCertificateValidationCallback =

                    (sender, certificate, chain, sslPolicyErrors) => true;

            }

        }



        public static bool Validate(object sender,

            X509Certificate certificate, X509Chain chain,

            SslPolicyErrors sslPolicyErrors)

        {

            var result = true;



            if (sslPolicyErrors == SslPolicyErrors.None)

            {

                return result;

            }



            if ((sslPolicyErrors &

                SslPolicyErrors.RemoteCertificateNameMismatch) != 0)

            {

                Console.WriteLine("SSL policy error {0}." +

                    " Make sure that your application is using the correct" +

                    " server host name.",

                    SslPolicyErrors.RemoteCertificateNameMismatch);

                result = result && false;

            }



            if ((sslPolicyErrors &

                SslPolicyErrors.RemoteCertificateChainErrors) != 0)

            {

                var chainStatusList = new List<string>();

                if (chain != null && chain.ChainStatus != null)

                {

                    foreach (var status in chain.ChainStatus)

                    {

                        if ((certificate.Subject == certificate.Issuer))

                        {

                            // Self signed certificates with an untrusted root

                            // are valid.

                            continue;

                        }

                        chainStatusList.Add(status.Status.ToString());

                    }

                }

                if (chainStatusList.Count > 0)

                {

                    Console.WriteLine(

                        "SSL policy error {0}. Fix the following errors {1}",

                        SslPolicyErrors.RemoteCertificateChainErrors,

                        string.Join(", ", chainStatusList));

                    result = result && false;

                }

            }



            if ((sslPolicyErrors &

                SslPolicyErrors.RemoteCertificateNotAvailable) != 0)

            {

                Console.WriteLine("SSL policy error {0}." +

                    " The server certificate is not available for validation.",

                    SslPolicyErrors.RemoteCertificateNotAvailable);

                result = result && false;

            }

            return result;

        }



        public static RequestSecurityTokenType GetHokRequestSecurityTokenType()

        {

            /**

            * For this request we need at least the following element in the

            * RequestSecurityTokenType set

            *

            * 1. Lifetime - represented by LifetimeType which specifies the

            * lifetime for the token to be issued

            *

            *  2. Tokentype - "urnoasisnamestcSAML20assertion", which is the

            * class that models the requested token

            *

            * 3. RequestType -

            * "httpdocsoasisopenorgwssxwstrust200512Issue", as we want

            * to get a token issued

            *

            * 4. KeyType -

            * "httpdocsoasisopenorgwssxwstrust200512PublicKey",

            * representing the kind of key the token will have. There are two

            * options namely bearer and holder-of-key

            *

            * 5. SignatureAlgorithm -

            * "httpwwww3org200104xmldsigmorersasha256", representing the

            * algorithm used for generating signature

            *

            * 6. Renewing - represented by the RenewingType which specifies whether

            * the token is renewable or not

            */

            RequestSecurityTokenType tokenType = new RequestSecurityTokenType();

            tokenType.TokenType = TokenTypeEnum.urnoasisnamestcSAML20assertion;

            tokenType.TokenTypeSpecified = true;

            tokenType.RequestType = RequestTypeEnum.httpdocsoasisopenorgwssxwstrust200512Issue;

            tokenType.KeyType = KeyTypeEnum.httpdocsoasisopenorgwssxwstrust200512PublicKey;

            tokenType.KeyTypeSpecified = true;

            tokenType.SignatureAlgorithm = SignatureAlgorithmEnum.httpwwww3org200104xmldsigmorersasha512;

            tokenType.SignatureAlgorithmSpecified = true;

            tokenType.Delegatable = true;

            tokenType.DelegatableSpecified = true;



            tokenType.Lifetime = GetLifetime(new TimeSpan(1, 10, 10));



            RenewingType renewing = new RenewingType();

            renewing.Allow = true;

            renewing.OK = true;

            tokenType.Renewing = renewing;

            return tokenType;

        }



        public static LifetimeType GetLifetime(TimeSpan? lifetimeSpan)

        {

            LifetimeType lifetime;



            if (lifetimeSpan.HasValue)

            {

                lifetime = new LifetimeType();

                AttributedDateTime created = new AttributedDateTime();

                String createdDate = String.Format(strDateFormat, DateTime.Now.ToUniversalTime());

                created.Value = createdDate;

                lifetime.Created = created;

                AttributedDateTime expires = new AttributedDateTime();

                String expireDate = String.Format(strDateFormat, DateTime.Now.Add(lifetimeSpan.Value).ToUniversalTime());

                expires.Value = expireDate;

                lifetime.Expires = expires;

            }

            else

            {

                lifetime = null;

            }

            return lifetime;

        }



        /// <summary>

        /// Prints basic information about the token

        /// </summary>

        /// <param name="token">SAML Token</param>

        public static void PrintToken(XmlElement token)

        {

            if (token != null)

            {

                String assertionId = token.Attributes.GetNamedItem(strAssertionId).Value;

                String issueInstanct = token.Attributes.GetNamedItem(strIssueInstant).Value;

                String typeOfToken = "";

                XmlNode subjectConfirmationNode = token.GetElementsByTagName(strSubjectConfirmationNode).Item(0);

                String subjectConfirmationMethodValue = subjectConfirmationNode.Attributes.GetNamedItem(strSubjectConfirmationMethodValueAttribute).Value;

                if (subjectConfirmationMethodValue == strSubjectConfirmationMethodValueTypeHoK)

                {

                    typeOfToken = "Holder-Of-Key";

                }

                else if (subjectConfirmationMethodValue == strSubjectConfirmationMethodValueTypeBearer)

                {

                    typeOfToken = "Bearer";

                }

                Console.WriteLine("Token Details");

                Console.WriteLine("\tAssertionId =  " + assertionId);

                Console.WriteLine("\tToken Type =  " + typeOfToken);

                Console.WriteLine("\tIssued On =  " + issueInstanct);

            }

        }



        public static X509Certificate2 GetCertificate()

        {

            X509Certificate2 signingCertificate = new X509Certificate2();

            string certificateFile = ConfigurationManager.AppSettings["PfxCertificateFile"];

            signingCertificate.Import(certificateFile, "", X509KeyStorageFlags.MachineKeySet);

            return signingCertificate;

        }



        public static STSService_PortTypeClient GetSTSService(

            string url, string username = null, string password = null,

            X509Certificate2 signingCertificate = null, XmlElement rawToken = null)

        {

            var binding = GetWcfBinding();

            var address = new EndpointAddress(url);



            STSService_PortTypeClient service =

               new STSService_PortTypeClient(binding, address);



            // Attach the behaviour that handles the WS-Trust 1.4 protocol for VMware SSO

            service.ChannelFactory.Endpoint.Behaviors.Add(new WsTrustBehavior(rawToken));



            SetCredentials(username, password, signingCertificate, service.ClientCredentials);



            return service;

        }



        # endregion

    }

}