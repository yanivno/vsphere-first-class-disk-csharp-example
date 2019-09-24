/*
 * ******************************************************
 * Copyright (c) VMware, Inc. 2010.  All Rights Reserved.
 * ******************************************************
 *
 * DISCLAIMER. THIS PROGRAM IS PROVIDED TO YOU "AS IS" WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, WHETHER ORAL OR WRITTEN,
 * EXPRESS OR IMPLIED. THE AUTHOR SPECIFICALLY DISCLAIMS ANY IMPLIED
 * WARRANTIES OR CONDITIONS OF MERCHANTABILITY, SATISFACTORY QUALITY,
 * NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.
 */
using System;
using System.Xml;

using Microsoft.Web.Services3;
using Microsoft.Web.Services3.Design;
using Microsoft.Web.Services3.Security;
using Microsoft.Web.Services3.Security.Tokens;


namespace AppUtil
{
    class CustomSecurityAssertion : SecurityPolicyAssertion
    {
        public String username;
        public String password;
        public XmlElement binaryToken;

        public String Username
        {
            get { return username; }
            set { username = value; }
        }

        public String Password
        {
            get { return password; }
            set { password = value; }
        }

        public XmlElement BinaryToken
        {
            get { return binaryToken; }
            set { binaryToken = value; }
        }

        public CustomSecurityAssertion()
            : base()
        { }

        public override SoapFilter CreateClientOutputFilter(FilterCreationContext context)
        {
            return new CustomSecurityClientOutputFilter(this);
        }

        public override SoapFilter CreateClientInputFilter(FilterCreationContext context)
        {
            return null;
        }

        public override SoapFilter CreateServiceInputFilter(FilterCreationContext context)
        {
            return null;
        }

        public override SoapFilter CreateServiceOutputFilter(FilterCreationContext context)
        {
            return null;
        }
    }

    class CustomSecurityClientOutputFilter : SendSecurityFilter
    {
        UsernameToken userToken = null;
        IssuedToken issuedToken = null;

        public CustomSecurityClientOutputFilter(CustomSecurityAssertion parentAssertion)
            : base(parentAssertion.ServiceActor, true)
        {
            String username = parentAssertion.username;
            String password = parentAssertion.password;
            XmlElement binaryToken = parentAssertion.binaryToken;
            if (binaryToken == null)
            {
                userToken = new UsernameToken(username.Trim(), password.Trim(), PasswordOption.SendPlainText);
            }
            else
            {
                issuedToken = new IssuedToken(binaryToken);
            }
        }

        /// <summary>
        ///  SecureMessage 
        /// </summary>
        /// <param name="envelope">SoapEnvelope</param>
        /// <param name="security">Security</param>
        public override void SecureMessage(SoapEnvelope envelope, Security security)
        {
            if (issuedToken == null)
            {
                security.Tokens.Add(userToken);
            }
            else
            {
                security.Tokens.Add(issuedToken);
            }
        }
    }
}
