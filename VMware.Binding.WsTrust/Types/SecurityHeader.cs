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
using System.Xml;
using System.Xml.Serialization;

namespace VMware.Binding.WsTrust.Types
{
   [Serializable]
   [XmlType(
      Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd",
      TypeName = "Security")]
   public class SecurityHeader
   {
      [XmlElement(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
      public Timestamp Timestamp { get; set; }

      [XmlAnyElement(Name = "UsernameToken")]
      public XmlElement UsernameToken { get; set; }

      [XmlAnyElement(Name = "BinarySecurityToken")]
      public XmlElement BinarySecurityToken { get; set; }

      [XmlAnyElement(Name = "Assertion", Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
      public XmlElement SamlToken { get; set; }

      [XmlAnyAttribute]
      public XmlAttribute[] AnyAttr { get; set; }
   }
}