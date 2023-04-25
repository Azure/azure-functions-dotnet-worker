// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    public class ServerlessHub<T> where T : class
    {
        private static readonly ObjectSerializer JsonObjectSerializer = new JsonObjectSerializer(new(JsonSerializerDefaults.Web));
        private static readonly NegotiationOptions DefaultNegotiateOptiosn = new();

        private readonly IServiceProvider _serviceProvider;

        private ServiceHubContext<T> HubContext
        {
            get
            {
                var type = GetType();
                var hubContextCache = _serviceProvider.GetRequiredService<HubContextCache>();
                if (hubContextCache.TryGetValue(type, out var hubContext))
                {
                    return (ServiceHubContext<T>)hubContext;
                }
                var hubContextProviderType = typeof(ServiceHubContextProvider<,>).MakeGenericType(type, typeof(T));
                var provider = _serviceProvider.GetRequiredService(hubContextProviderType);
                var property = hubContextProviderType.GetProperty("ServiceHubContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
                var value = property.GetValue(provider);
                hubContextCache.Add(type, value);
                return (ServiceHubContext<T>)value;
            }
        }

        /// <summary>
        /// Gets an object that can be used to invoke methods on the clients connected to this hub.
        /// </summary>
        protected IHubClients<T> Clients => HubContext.Clients;

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
            return JsonObjectSerializer.Serialize(negotiateResponse);
        }

        protected ServerlessHub(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
