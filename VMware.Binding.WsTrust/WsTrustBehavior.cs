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

using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace VMware.Binding.WsTrust
{
    /// <summary>
    ///    Endpoint behavior that implements the WS-Security protocol for
    ///    VMware SSO authentication.
    /// </summary>
    public class WsTrustBehavior : IEndpointBehavior
    {
        private readonly XmlElement _samlToken;

        public WsTrustBehavior(XmlElement samlToken)
        {
            _samlToken = samlToken;
        }

        public WsTrustBehavior()
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            var clientCredentials = endpoint.Behaviors.Find<ClientCredentials>();

            // The protocol is implemented as a message inspector (in this case interceptor).
            clientRuntime.MessageInspectors.Add(new WsTrustClientMessageInspector(clientCredentials, _samlToken));
        }
    }
}