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



namespace AppUtil

{

    using System;

    using System.IO;

    using System.Net;

    using System.Net.Security;

    using System.Runtime.Serialization.Formatters.Binary;

    using System.Security.Cryptography;

    using System.Security.Cryptography.X509Certificates;

    using System.ServiceModel;

    using System.ServiceModel.Channels;

    using System.Xml;

    using Vim25Api;

    using vmware.sso;

    using VMware.Binding.WsTrust;

    /// <summary>

    /// Connection Handler for WebService

    /// </summary>

    public class SvcConnection

    {



        public enum ConnectionState

        {

            Connected,

            Disconnected,

        }



        public VimPortType _service;

        protected ConnectionState _state;

        public ServiceContent _sic;

        protected ManagedObjectReference _svcRef;

        public event ConnectionEventHandler AfterConnect;

        public event ConnectionEventHandler AfterDisconnect;

        public event ConnectionEventHandler BeforeDisconnect;

        public RequestSecurityTokenResponseType RequestSecurityTokenResponse

        { get; private set; }

        public XmlElement SamlTokenXml { get; private set; }

        public X509Certificate2 X509Certificate { get; private set; }

        public UserSession VimUserSession { get; private set; }

        public AsymmetricAlgorithm PrivateKey

        {

            get

            {

                if (X509Certificate != null)

                    return X509Certificate.PrivateKey;

                return null;

            }

        }



        private bool _ignoreCert;

        public bool ignoreCert

        {

            get { return _ignoreCert; }

            set

            {

                if (value)

                {

                    ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

                }

                _ignoreCert = value;

            }

        }

        /// <summary>

        ///  This method is used to validate remote certificate 

        /// </summary>

        /// <param name="sender">string Array</param>

        /// <param name="certificate">X509Certificate certificate</param>

        /// <param name="chain">X509Chain chain</param>

        /// <param name="policyErrors">SslPolicyErrors policyErrors</param>

        private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)

        {

            return true;

        }



        public SvcConnection(string svcRefVal)

        {

            _state = ConnectionState.Disconnected;

            if (ignoreCert)

            {

                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            }

            _svcRef = new ManagedObjectReference();

            _svcRef.type = "ServiceInstance";

            _svcRef.Value = svcRefVal;

        }





        /// <summary>

        /// Creates an instance of the VMA proxy and establishes a connection

        /// </summary>

        /// <param name="url"></param>

        /// <param name="username"></param>

        /// <param name="password"></param>

        public void Connect(string url, string username, string password)

        {

            if (_service != null)

            {

                Disconnect();

            }



            _service = GetVimService(url, username, password);

            _sic = _service.RetrieveServiceContent(_svcRef);



            if (_sic.sessionManager != null)

            {

                _service.Login(_sic.sessionManager, username, password, null);

            }



            _state = ConnectionState.Connected;

            if (AfterConnect != null)

            {

                AfterConnect(this, new ConnectionEventArgs());

            }

        }



        /// <summary>

        /// Establishe a connection using an existing cookie

        /// </summary>

        /// <param name="url">Server Url</param>

        /// <param name="cookie">Cookie used to connect to the server</param>

        public void Connect(string url, Cookie cookie)

        {

            if (_service != null)

            {

                Disconnect();

            }



            _service = GetVimService(url);

            _sic = _service.RetrieveServiceContent(_svcRef);



            // Add the cookie to the cookie manager

            var cookieManager = ((IContextChannel)_service).GetProperty<IHttpCookieContainerManager>();

            cookieManager.CookieContainer.SetCookies(new Uri(url), cookie.ToString());



            _state = ConnectionState.Connected;

            if (AfterConnect != null)

            {

                AfterConnect(this, new ConnectionEventArgs());

            }

        }



        public void SaveSession(String fileName, String urlString)

        {

            BinaryFormatter bf = new BinaryFormatter();

            Stream s = File.Open(fileName, FileMode.Create);

            var cookieManager = ((IContextChannel)_service).GetProperty<IHttpCookieContainerManager>();

            var cookie = cookieManager.CookieContainer.GetCookies(new Uri(urlString))[0];

            bf.Serialize(s, cookie);

            s.Close();

        }



        public void LoadSession(String fileName, String urlString)

        {

            BinaryFormatter bf = new BinaryFormatter();

            Stream s = File.Open(fileName, FileMode.Open);

            Cookie cookie = bf.Deserialize(s) as Cookie;

            s.Close();

            Connect(urlString, cookie);

        }



        public VimPortType Service

        {

            get

            {

                return _service;

            }

        }



        public ManagedObjectReference ServiceRef

        {

            get

            {

                return _svcRef;

            }

        }



        public ServiceContent ServiceContent

        {

            get

            {

                return _sic;

            }

        }



        public ManagedObjectReference PropCol

        {

            get

            {

                return _sic.propertyCollector;

            }

        }



        public ManagedObjectReference Root

        {

            get

            {

                return _sic.rootFolder;

            }

        }



        public ConnectionState State

        {

            get

            {

                return _state;

            }

        }



        /// <summary>

        /// Disconnects the Connection

        /// </summary>

        public void Disconnect()

        {

            if (_service != null)

            {

                if (BeforeDisconnect != null)

                {

                    BeforeDisconnect(this, new ConnectionEventArgs());

                }



                if (_sic != null)

                    _service.Logout(_sic.sessionManager);



                _service = null;

                _sic = null;



                _state = ConnectionState.Disconnected;

                if (AfterDisconnect != null)

                {

                    AfterDisconnect(this, new ConnectionEventArgs());

                }

            }

        }



        public void SSOConnect(RequestSecurityTokenResponseType token, string url)

        {

            if (_service != null)

            {

                Disconnect();

            }



            _service = GetVimService(url, null, null, null, token.RequestedSecurityToken);

            _sic = _service.RetrieveServiceContent(this._svcRef);

            if (ServiceContent.sessionManager != null)

            {

                var userSession = _service.LoginByToken(ServiceContent.sessionManager, null);

                VimUserSession = userSession;

            }



            _state = ConnectionState.Connected;

            if (AfterConnect != null)

            {

                AfterConnect(this, new ConnectionEventArgs());

            }

        }



        private static VimPortType GetVimService(

            string url, string username = null, string password = null,

            X509Certificate2 signingCertificate = null, XmlElement rawToken = null)

        {

            var binding = SamlTokenHelper.GetWcfBinding();

            var address = new EndpointAddress(url);



            var factory = new ChannelFactory<VimPortType>(binding, address);



            // Attach the behaviour that handles the WS-Trust 1.4 protocol for VMware Vim Service

            factory.Endpoint.Behaviors.Add(new WsTrustBehavior(rawToken));



            SamlTokenHelper.SetCredentials(username, password, signingCertificate, factory.Credentials);



            var service = factory.CreateChannel();



            return service;

        }

    }



    public class ConnectionEventArgs : System.EventArgs

    {

    }



    public delegate void ConnectionEventHandler(object sender, ConnectionEventArgs e);





}

