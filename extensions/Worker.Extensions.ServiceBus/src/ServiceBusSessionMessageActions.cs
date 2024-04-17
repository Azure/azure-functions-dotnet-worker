using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.ServiceBus.Grpc;

namespace Microsoft.Azure.Functions.Worker
{
    [InputConverter(typeof(ServiceBusSessionMessageActionsConverter))]
    public class ServiceBusSessionMessageActions
    {
        private readonly Settlement.SettlementClient _settlement;

        private readonly string _sessionId;

        internal ServiceBusSessionMessageActions(Settlement.SettlementClient settlement, string sessionId)
        {
            _settlement = settlement;
            _sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageActions"/> class for mocking use in testing.
        /// </summary>
        /// <remarks>
        /// This constructor exists only to support mocking. When used, class state is not fully initialized, and
        /// will not function correctly; virtual members are meant to be mocked.
        ///</remarks>
        protected ServiceBusSessionMessageActions()
        {
            _settlement = null!; // not expected to be used during mocking.
        }

        public virtual DateTimeOffset SessionLockedUntil
        {
            get
            {
                var request = new SessionLockedUntil()
                {
                    SessionId = _sessionId,
                };
                var sessionLockedRequest = _settlement.SessionLocked(request);
                return sessionLockedRequest.LockedUntil.ToDateTimeOffset();
            }
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task<BinaryData> GetSessionStateAsync(
            CancellationToken cancellationToken = default)
        {
            var request = new GetRequest()
            {
                SessionId = _sessionId,
            };

            GetResponse result = await _settlement.GetAsync(request, cancellationToken: cancellationToken);
            byte[] byteArray = result.SessionState.ToByteArray();
            BinaryData binaryData = BinaryData.FromBytes(byteArray);
            return await Task.FromResult(binaryData);
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task SetSessionStateAsync(
            BinaryData sessionState,
            CancellationToken cancellationToken = default)
        {
            var request = new SetRequest()
            {
                SessionId = _sessionId,
                SessionState = ByteString.CopyFrom(sessionState.ToArray()),
            };

            await _settlement.SetAsync(request, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task ReleaseSession(
            CancellationToken cancellationToken = default)
        {
            var request = new ReleaseSession()
            {
                SessionId = _sessionId,
            };

            await _settlement.ReleaseAsync(request, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task RenewSessionLockAsync(
            CancellationToken cancellationToken = default)
        {
            var request = new RenewSessionLock()
            {
                SessionId = _sessionId,
            };

            await _settlement.RenewAsync(request, cancellationToken: cancellationToken);
        }
    }
}
