// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Azure.ServiceBus.Grpc;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests
{
    public class ServiceBusSessionMessageActionsTests
    {
        [Fact]
        public async Task CanGetSessionState()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid(), sessionId: "test");
            var messageActions = new ServiceBusSessionMessageActions(new MockSettlementClient(message.SessionId), message.SessionId, message.LockedUntil);
            await messageActions.GetSessionStateAsync();
        }

        [Fact]
        public async Task CanSetSessionState()
        {
            byte[] predefinedData = { 0x48, 0x65 };
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid(), sessionId: "test");
            var messageActions = new ServiceBusSessionMessageActions(new MockSettlementClient(message.SessionId, ByteString.CopyFrom(predefinedData)), message.SessionId, message.LockedUntil);
            await messageActions.SetSessionStateAsync(BinaryData.FromBytes(predefinedData));
        }

        [Fact]
        public async Task CanReleaseSession()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid(), sessionId: "test");
            var messageActions = new ServiceBusSessionMessageActions(new MockSettlementClient(message.SessionId), message.SessionId, message.LockedUntil);
            await messageActions.ReleaseSession();
        }

        [Fact]
        public async Task CanRenewSessionLock()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid(), sessionId: "test");
            var messageActions = new ServiceBusSessionMessageActions(new MockSettlementClient(message.SessionId), message.SessionId, message.LockedUntil);
            await messageActions.RenewSessionLockAsync();
        }
    }
}
