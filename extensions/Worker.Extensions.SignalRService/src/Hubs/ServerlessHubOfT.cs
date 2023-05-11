// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    public abstract class ServerlessHub<T> : ServerlessHub where T : class
    {
        protected ServerlessHub(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        private ServiceHubContext<T> HubContext
        {
            get
            {
                var type = GetType();
                var hubContextProvider = ServiceProvider.GetRequiredService<HubContextProvider>();
                if (hubContextProvider.TryGetValue(type, out var hubContext))
                {
                    return (ServiceHubContext<T>)hubContext;
                }
                else
                {
                    throw new InvalidOperationException($"The serverlesshub {type.Name} is not registered using services.AddServerlessHub<T>().");
                }
            }
        }

        /// <summary>
        /// Gets an object that can be used to invoke methods on the clients connected to this hub.
        /// </summary>
        protected new IHubClients<T> Clients => HubContext.Clients;

        /// <summary>
        /// Get the group manager of this hub.
        /// </summary>
        protected new GroupManager Groups => HubContext.Groups;

        /// <summary>
        /// Get the user group manager of this hub.
        /// </summary>
        protected new UserGroupManager UserGroups => HubContext.UserGroups;

        /// <summary>
        /// Get the client manager of this hub.
        /// </summary>
        protected new ClientManager ClientManager => HubContext.ClientManager;

        /// <summary>
        /// Gets client endpoint access information object for SignalR hub connections to connect to Azure SignalR Service
        /// </summary>
        protected new async Task<BinaryData> NegotiateAsync(NegotiationOptions? options = null)
        {
            var negotiateResponse = await HubContext.NegotiateAsync(options ?? DefaultNegotiateOptiosn);
            return ObjectSerializer.Serialize(negotiateResponse);
        }
    }
}
