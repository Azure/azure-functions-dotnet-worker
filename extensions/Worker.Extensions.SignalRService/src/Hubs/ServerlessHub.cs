// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    public abstract class ServerlessHub
    {
        protected ServerlessHub(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        internal static readonly ObjectSerializer ObjectSerializer = new JsonObjectSerializer(new(JsonSerializerDefaults.Web));
        internal readonly NegotiationOptions DefaultNegotiateOptiosn = new();
        internal IServiceProvider ServiceProvider { get; }

        private ServiceHubContext HubContext
        {
            get
            {
                var type = GetType();
                var hubContextCache = ServiceProvider.GetRequiredService<HubContextProvider>();
                if (hubContextCache.TryGetValue(type, out var hubContext))
                {
                    return (ServiceHubContext)hubContext;
                }
                else
                {
                    throw new InvalidOperationException($"The serverlesshub {type.Name} is not registered using services.AddServerlessHub().");
                }
            }
        }

        /// <summary>
        /// Gets an object that can be used to invoke methods on the clients connected to this hub.
        /// </summary>
        protected IHubClients Clients => HubContext.Clients;

        /// <summary>
        /// Get the group manager of this hub.
        /// </summary>
        protected GroupManager Groups => HubContext.Groups;

        /// <summary>
        /// Get the user group manager of this hub.
        /// </summary>
        protected UserGroupManager UserGroups => HubContext.UserGroups;

        /// <summary>
        /// Get the client manager of this hub.
        /// </summary>
        protected ClientManager ClientManager => HubContext.ClientManager;

        /// <summary>
        /// Gets client endpoint access information object for SignalR hub connections to connect to Azure SignalR Service
        /// </summary>
        protected async Task<BinaryData> NegotiateAsync(NegotiationOptions? options = null)
        {
            var negotiateResponse = await HubContext.NegotiateAsync(options ?? DefaultNegotiateOptiosn);
            return ObjectSerializer.Serialize(negotiateResponse);
        }

        [AttributeUsage(AttributeTargets.Class)]
        protected internal class SignalRConnectionAttribute : Attribute
        {
            public SignalRConnectionAttribute(string connectionName = Constants.AzureSignalRConnectionStringName)
            {
                ConnectionName = connectionName;
            }

            public string ConnectionName { get; }
        }
    }
}
