/*

 ********************************************************

 * Copyright (c) VMware, Inc.2010, 2016. All Rights Reserved.

 ********************************************************

 *

 * DISCLAIMER. THIS PROGRAM IS PROVIDED TO YOU "AS IS" WITHOUT

 * WARRANTIES OR CONDITIONS OF ANY KIND, WHETHER ORAL OR WRITTEN,

 * EXPRESS OR IMPLIED. THE AUTHOR SPECIFICALLY DISCLAIMS ANY IMPLIED

 * WARRANTIES OR CONDITIONS OF MERCHANTABILITY, SATISFACTORY QUALITY,

 * NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.

 */



namespace AcquireBearerTokenByUserCredentialSample

{

    using System;

    using System.Collections.Generic;

    using System.Configuration;

    using System.Net;

    using System.Net.Security;

    using System.Security.Cryptography.X509Certificates;

    using System.ServiceModel;

    using System.ServiceModel.Channels;

    using System.Xml;

    using vmware.sso;



    public class AcquireBearerTokenByUserCredential

    {

        # region variable declaration

        static string strAssertionId = "ID";

        static string strIssueInstant = "IssueInstant";

        static string strSubjectConfirmationNode = "saml2:SubjectConfirmation";

        static string strSubjectConfirmationMethodValueAttribute = "Method";

        static string strSubjectConfirmationMethodValueTypeBearer = "urn:oasis:names:tc:SAML:2.0:cm:bearer";

        static string strSubjectConfirmationMethodValueTypeHoK = "urn:oasis:names:tc:SAML:2.0:cm:holder-of-key";

        static string strDateFormat = "{0:yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'}";

        # endregion



        # region private function Definition



        /// <summary>

        ///  This method is used to print message if there is insufficient parameter 

        /// </summary>

        private static void PrintUsage()

        {

            Console.WriteLine("AcquireBearerTokenByUserCredentialSample [sso url] [username] [password]");

        }



        /// <summary>

        ///  This method ignores the server certificate validation

        ///  THIS IS ONLY FOR SAMPLES USE. PROVIDE PROPER VALIDATION FOR PRODUCTION CODE.

        /// </summary>

        /// <param name="sender">string Array</param>

        /// <param name="certificate">X509Certificate certificate</param>

        /// <param name="chain">X509Chain chain</param>

        /// <param name="policyErrors">SslPolicyErrors policyErrors</param>

        private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)

        {

            return true;

        }



        /// <summary>

        /// Prints basic information about the token

        /// </summary>

        /// <param name="token">SAML Token</param>

        private static void PrintToken(XmlElement token)

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



        public static Binding GetCustomBinding()

        {

            var customBinding = new CustomBinding();



            var security = SecurityBindingElement.CreateUserNameOverTransportBindingElement();

            security.EnableUnsecuredResponse = true;

            security.IncludeTimestamp = true;

            security.AllowInsecureTransport = true;



            var textMessageEncoding = new TextMessageEncodingBindingElement();

            textMessageEncoding.MessageVersion = MessageVersion.Soap11;



            var transport = new HttpsTransportBindingElement();



            customBinding.Elements.Add(security);

            customBinding.Elements.Add(textMessageEncoding);

            customBinding.Elements.Add(transport);



            return customBinding;

        }



        private static void SetupServerCertificateValidation()

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



        # endregion



        # region public function definition



        /// <summary>

        ///  This method is used to get bearer Token

        /// </summary>

        /// <param name="args">string Array [sso url] [username] [password]</param>

        public static RequestSecurityTokenResponseType GetBearerToken(string url, string username, string password)

        {

            var binding = GetCustomBinding();

            var address = new EndpointAddress(url);



            var stsServiceClient = new STSService_PortTypeClient(binding, address);



            stsServiceClient.ClientCredentials.UserName.UserName = username;

            stsServiceClient.ClientCredentials.UserName.Password = password;



            RequestSecurityTokenType tokenType = new RequestSecurityTokenType();



            /**

            * For this request we need at least the following element in the

            * RequestSecurityTokenType set

            *

            * 1. Lifetime - represented by LifetimeType which specifies the

            * lifetime for the token to be issued

            *

            * 2. Tokentype - "urnoasisnamestcSAML20assertion", which is the

            * class that models the requested token

            *

            * 3. RequestType -

            * "httpdocsoasisopenorgwssxwstrust200512Issue", as we want

            * to get a token issued

            *

            * 4. KeyType -

            * "httpdocsoasisopenorgwssxwstrust200512Bearer",

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

            tokenType.TokenType = TokenTypeEnum.urnoasisnamestcSAML20assertion;

            tokenType.TokenTypeSpecified = true;

            tokenType.RequestType = RequestTypeEnum.httpdocsoasisopenorgwssxwstrust200512Issue;

            tokenType.KeyType = KeyTypeEnum.httpdocsoasisopenorgwssxwstrust200512Bearer;

            tokenType.KeyTypeSpecified = true;

            tokenType.SignatureAlgorithm = SignatureAlgorithmEnum.httpwwww3org200104xmldsigmorersasha256;

            tokenType.SignatureAlgorithmSpecified = true;

            tokenType.Delegatable = true;

            tokenType.DelegatableSpecified = true;



            LifetimeType lifetime = new LifetimeType();

            AttributedDateTime created = new AttributedDateTime();

            String createdDate = String.Format(strDateFormat, DateTime.Now.ToUniversalTime());

            created.Value = createdDate;

            lifetime.Created = created;



            AttributedDateTime expires = new AttributedDateTime();

            TimeSpan duration = new TimeSpan(1, 10, 10);

            String expireDate = String.Format(strDateFormat, DateTime.Now.Add(duration).ToUniversalTime());

            expires.Value = expireDate;

            lifetime.Expires = expires;

            tokenType.Lifetime = lifetime;

            RenewingType renewing = new RenewingType();

            renewing.Allow = false;

            renewing.OK = true;

            tokenType.Renewing = renewing;



            try

            {

                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                SetupServerCertificateValidation();

                RequestSecurityTokenResponseCollectionType responseToken = stsServiceClient.Issue(tokenType);

                RequestSecurityTokenResponseType rstResponse = responseToken.RequestSecurityTokenResponse;

                return rstResponse;

            }

            catch (Exception ex)

            {

                Console.WriteLine(ex.ToString());

                throw ex;

            }

        }





        /// <summary>

        /// Main function of the application

        /// </summary>

        /// <param name="args">string args [sso url] [username] [password]</param>

        public static void Main(string[] args)

        {

            if (args.Length < 3)

            {

                PrintUsage();

            }

            else

            {

                PrintToken(GetBearerToken(args[0], args[1], args[2]).RequestedSecurityToken);

            }

            Console.WriteLine("Press Any Key To Exit.");

            Console.ReadLine();

        }



        # endregion

    }

}







