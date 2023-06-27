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
    /// <inheritdoc/>
    public abstract class ServerlessHub<T> : ServerlessHub where T : class
    {
        /// <summary>
        /// Constructor used by the Azure Functions runtime to create an instance of the <see cref="ServerlessHub{T}"/> class. 
        /// </summary>
        [ActivatorUtilitiesConstructor]
        protected ServerlessHub(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Constructor for unit test.
        /// </summary>
        /// <param name="serviceHubContext">A mocked service hub context object.</param>
        protected ServerlessHub(ServiceHubContext<T> serviceHubContext) : base(serviceHubContext)
        {
        }

        private ServiceHubContext<T> HubContext => _hubContext as ServiceHubContext<T> ?? throw new InvalidOperationException($"The serverlesshub {GetType().Name} is not registered correctly using services.AddServerlessHub().");

        /// <inheritdoc/>
        protected new virtual IHubClients<T> Clients => HubContext.Clients;

        /// <inheritdoc/>
        protected override GroupManager Groups => HubContext.Groups;

        /// <inheritdoc/>
        protected override UserGroupManager UserGroups => HubContext.UserGroups;

        /// <inheritdoc/>
        protected override ClientManager ClientManager => HubContext.ClientManager;

        /// <inheritdoc/>
        protected override async Task<BinaryData> NegotiateAsync(NegotiationOptions? options = null)
        {
            var negotiateResponse = await HubContext.NegotiateAsync(options ?? DefaultNegotiateOptiosn);
            return ObjectSerializer.Serialize(negotiateResponse);
        }
    }
}
