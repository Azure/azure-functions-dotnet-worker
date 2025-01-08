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

        private class MockSettlementClient : Settlement.SettlementClient
        {
            private readonly string _sessionId;
            private readonly ByteString _sessionState;
            public MockSettlementClient(string sessionId, ByteString sessionState = null) : base()
            {
                _sessionId = sessionId;
                _sessionState = sessionState;
            }

            public override AsyncUnaryCall<GetSessionStateResponse> GetSessionStateAsync(GetSessionStateRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
            {
                Assert.Equal(_sessionId, request.SessionId);
                return new AsyncUnaryCall<GetSessionStateResponse>(Task.FromResult(new GetSessionStateResponse()), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }

            public override AsyncUnaryCall<Empty> SetSessionStateAsync(SetSessionStateRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
            {
                Assert.Equal(_sessionId, request.SessionId);
                Assert.Equal(_sessionState, request.SessionState);
                return new AsyncUnaryCall<Empty>(Task.FromResult(new Empty()), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }

            public override AsyncUnaryCall<Empty> ReleaseSessionAsync(ReleaseSessionRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
            {
                Assert.Equal(_sessionId, request.SessionId);
                return new AsyncUnaryCall<Empty>(Task.FromResult(new Empty()), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }

            public override AsyncUnaryCall<RenewSessionLockResponse> RenewSessionLockAsync(RenewSessionLockRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
            {
                Assert.Equal(_sessionId, request.SessionId);
                var response = new RenewSessionLockResponse();
                response.LockedUntil = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(30));
                return new AsyncUnaryCall<RenewSessionLockResponse>(Task.FromResult(response), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }
        }
    }
}
