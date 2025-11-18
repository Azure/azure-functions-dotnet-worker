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
    public class ServiceBusMessageActionsTests
    {
        [Fact]
        public async Task CanCompleteMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid());
            var messageActions = new ServiceBusMessageActions(new MockSettlementClient(message.LockToken));
            await messageActions.CompleteMessageAsync(message);
        }

        [Fact]
        public async Task CanAbandonMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid());
            var properties = new Dictionary<string, object>()
            {
                { "int", 1 },
                { "string", "foo"},
                { "timespan", TimeSpan.FromSeconds(1) },
                { "datetime", DateTime.UtcNow },
                { "datetimeoffset", DateTimeOffset.UtcNow },
                { "guid", Guid.NewGuid() }
            };
            var messageActions = new ServiceBusMessageActions(new MockSettlementClient(message.LockToken, properties));
            await messageActions.AbandonMessageAsync(message, properties);
        }

        [Fact]
        public async Task CanDeadLetterMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid());
            var properties = new Dictionary<string, object>()
            {
                { "int", 1 },
                { "string", "foo"},
                { "timespan", TimeSpan.FromSeconds(1) },
                { "datetime", DateTime.UtcNow },
                { "datetimeoffset", DateTimeOffset.UtcNow },
                { "guid", Guid.NewGuid() }
            };
            var messageActions = new ServiceBusMessageActions(new MockSettlementClient(message.LockToken, properties));
            await messageActions.DeadLetterMessageAsync(message, properties);
        }

        [Fact]
        public async Task CanDeferMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid());
            var properties = new Dictionary<string, object>()
            {
                { "int", 1 },
                { "string", "foo"},
                { "timespan", TimeSpan.FromSeconds(1) },
                { "datetime", DateTime.UtcNow },
                { "datetimeoffset", DateTimeOffset.UtcNow },
                { "guid", Guid.NewGuid() }
            };
            var messageActions = new ServiceBusMessageActions(new MockSettlementClient(message.LockToken, properties));
            await messageActions.DeferMessageAsync(message, properties);
        }

        [Fact]
        public async Task CanRenewMessageLock()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: Guid.NewGuid());
            var properties = new Dictionary<string, object>()
            {
                { "int", 1 },
                { "string", "foo"},
                { "timespan", TimeSpan.FromSeconds(1) },
                { "datetime", DateTime.UtcNow },
                { "datetimeoffset", DateTimeOffset.UtcNow },
                { "guid", Guid.NewGuid() }
            };
            var messageActions = new ServiceBusMessageActions(new MockSettlementClient(message.LockToken, properties));
            await messageActions.RenewMessageLockAsync(message);
        }

        [Fact]
        public async Task PassingNullMessageThrows()
        {
            var messageActions = new ServiceBusMessageActions(new MockSettlementClient(null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await messageActions.CompleteMessageAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await messageActions.AbandonMessageAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await messageActions.DeadLetterMessageAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await messageActions.DeferMessageAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await messageActions.RenewMessageLockAsync(null));
        }

        internal class MockSettlementClient : Settlement.SettlementClient
        {
            private readonly string _lockToken;
            private readonly ByteString _propertiesToModify;
            public MockSettlementClient(string lockToken, IDictionary<string, object> propertiesToModify = default) : base()
            {
                _lockToken = lockToken;
                if (propertiesToModify != null)
                {
                    _propertiesToModify = ServiceBusMessageActions.ConvertToByteString(propertiesToModify);
                }
            }

            public override AsyncUnaryCall<Empty> CompleteAsync(CompleteRequest request, Metadata headers = null, DateTime? deadline = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                Assert.Equal(_lockToken, request.Locktoken);
                return new AsyncUnaryCall<Empty>(Task.FromResult(new Empty()), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }

            public override AsyncUnaryCall<Empty> AbandonAsync(AbandonRequest request, Metadata headers = null, DateTime? deadline = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                Assert.Equal(_lockToken, request.Locktoken);
                Assert.Equal(_propertiesToModify, request.PropertiesToModify);
                return new AsyncUnaryCall<Empty>(Task.FromResult(new Empty()), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }

            public override AsyncUnaryCall<Empty> DeadletterAsync(DeadletterRequest request, Metadata headers = null, DateTime? deadline = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                Assert.Equal(_lockToken, request.Locktoken);
                Assert.Equal(_propertiesToModify, request.PropertiesToModify);
                return new AsyncUnaryCall<Empty>(Task.FromResult(new Empty()), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }

            public override AsyncUnaryCall<Empty> DeferAsync(DeferRequest request, Metadata headers = null, DateTime? deadline = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                Assert.Equal(_lockToken, request.Locktoken);
                Assert.Equal(_propertiesToModify, request.PropertiesToModify);
                return new AsyncUnaryCall<Empty>(Task.FromResult(new Empty()), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }

            public override AsyncUnaryCall<Empty> RenewMessageLockAsync(RenewMessageLockRequest request, CallOptions options)
            {
                Assert.Equal(_lockToken, request.Locktoken);
                return new AsyncUnaryCall<Empty>(Task.FromResult(new Empty()), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }
        }
    }
}
