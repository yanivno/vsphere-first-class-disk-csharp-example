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
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VMware.Binding.WsTrust.Types
{
   [Serializable]
   [XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
   public class AttributedDateTime
   {
      [XmlAttribute(Form = XmlSchemaForm.Qualified, DataType = "ID")]
      public string Id { get; set; }

      [XmlAnyAttribute]
      public XmlAttribute[] AnyAttr { get; set; }

      [XmlText]
      public string StringValue { get; set; }

      [XmlIgnore]
      public DateTime Value {
         set { StringValue = Util.ToXmlDateTime(value); }
      }
   }
}