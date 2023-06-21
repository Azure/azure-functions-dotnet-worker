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
        public const string DefaultConnectionStringName = "AzureSignalRConnectionString";

        internal static readonly ObjectSerializer ObjectSerializer = new JsonObjectSerializer(new(JsonSerializerDefaults.Web));
        internal static readonly NegotiationOptions DefaultNegotiateOptiosn = new();
        internal readonly object? _hubContext;

        [ActivatorUtilitiesConstructor]
        protected ServerlessHub(IServiceProvider serviceProvider)
        {
            serviceProvider.GetService<HubContextProvider>()?.TryGetValue(GetType(), out _hubContext);
        }

        /// <summary>
        /// Constructor for unit test.
        /// </summary>
        /// <param name="serviceHubContext">A mocked service hub context object.</param>
        protected ServerlessHub(ServiceHubContext serviceHubContext)
        {
            _hubContext = serviceHubContext;
        }

        internal ServerlessHub(object? hubContext)
        {
            _hubContext = hubContext;
        }

        private ServiceHubContext HubContext => _hubContext as ServiceHubContext ?? throw new InvalidOperationException($"The serverlesshub {GetType().Name} is not registered correctly using services.AddServerlessHub().");

        /// <summary>
        /// Gets an abstraction that provides access to client connections.
        /// </summary>
        protected virtual IHubClients Clients => HubContext.Clients;

        /// <summary>
        /// Gets the group manager of this hub.
        /// </summary>
        protected virtual GroupManager Groups => HubContext.Groups;

        /// <summary>
        /// Gets the user group manager of this hub.
        /// </summary>
        protected virtual UserGroupManager UserGroups => HubContext.UserGroups;

        /// <summary>
        /// Gets the client manager of this hub.
        /// </summary>
        protected virtual ClientManager ClientManager => HubContext.ClientManager;

        /// <summary>
        /// Gets client endpoint access information object for SignalR hub connections to connect to Azure SignalR Service
        /// </summary>
        protected virtual async Task<BinaryData> NegotiateAsync(NegotiationOptions? options = null)
        {
            var negotiateResponse = await HubContext.NegotiateAsync(options ?? DefaultNegotiateOptiosn);
            return ObjectSerializer.Serialize(negotiateResponse);
        }

        [AttributeUsage(AttributeTargets.Class)]
        protected internal class SignalRConnectionAttribute : Attribute
        {
            public SignalRConnectionAttribute(string connectionName = DefaultConnectionStringName)
            {
                ConnectionName = connectionName;
            }

            public string ConnectionName { get; }
        }
    }
}
