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

using System.Security.Cryptography.Xml;
using System.Xml;

namespace VMware.Binding.WsTrust
{
    /// <summary>
    ///    A <see cref="SignedXml" /> implementation that can
    ///    reference elements identified by an WS-Security Id.
    /// </summary>
    internal class WsuSignedXml : SignedXml
    {
        private const string WsuNamespace =
           "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        public WsuSignedXml(XmlDocument xml)
            : base(xml) { }

        public WsuSignedXml(XmlElement xmlElement)
            : base(xmlElement) { }

        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            // Check if there is a standard ID attribute
            XmlElement idElem = base.GetIdElement(document, idValue);

            if (idElem == null)
            {
                XmlNamespaceManager nsManager = new XmlNamespaceManager(document.NameTable);
                nsManager.AddNamespace("wsu", WsuNamespace);

                // An xpath that matches elements with the specified wsd:Id attribute value.
                string xpath = string.Format("//*[@wsu:Id=\"{0}\"]", idValue);

                idElem = document.SelectSingleNode(xpath, nsManager) as XmlElement;
            }

            return idElem;
        }
    }
}