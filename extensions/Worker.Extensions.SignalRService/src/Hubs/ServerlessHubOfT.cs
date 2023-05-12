// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    /// <inheritdoc/>
    public abstract class ServerlessHub<T> : ServerlessHub where T : class
    {
        protected ServerlessHub(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        private ServiceHubContext<T> HubContext => _hubContext as ServiceHubContext<T> ?? throw new InvalidOperationException($"The serverlesshub {GetType().Name} is not registered correctly using services.AddServerlessHub().");

        protected new virtual IHubClients<T> Clients => HubContext.Clients;

        protected override GroupManager Groups => HubContext.Groups;

        protected override UserGroupManager UserGroups => HubContext.UserGroups;

        protected override ClientManager ClientManager => HubContext.ClientManager;

        protected override async Task<BinaryData> NegotiateAsync(NegotiationOptions? options = null)
        {
            var negotiateResponse = await HubContext.NegotiateAsync(options ?? DefaultNegotiateOptiosn);
            return ObjectSerializer.Serialize(negotiateResponse);
        }
    }
}
