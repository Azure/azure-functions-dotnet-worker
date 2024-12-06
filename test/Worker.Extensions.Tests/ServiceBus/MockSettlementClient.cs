using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Azure.ServiceBus.Grpc;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.ServiceBus
{
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
