// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.ServiceBus.Grpc;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to <see cref="ServiceBusSessionMessageActions" /> type parameters.
    /// </summary>
    [InputConverter(typeof(ServiceBusSessionMessageActionsConverter))]
    public class ServiceBusSessionMessageActions
    {
        private readonly Settlement.SettlementClient _settlement;
        private readonly string _sessionId;

        internal ServiceBusSessionMessageActions(Settlement.SettlementClient settlement, string sessionId, DateTimeOffset sessionLockedUntil)
        {
            _settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
            _sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            SessionLockedUntil = sessionLockedUntil;
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
            _sessionId = null!; // not expected to be used during mocking.
        }

        public virtual DateTimeOffset SessionLockedUntil { get; protected set; }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task<BinaryData?> GetSessionStateAsync(
            CancellationToken cancellationToken = default)
        {
            var request = new GetSessionStateRequest()
            {
                SessionId = _sessionId,
            };

            GetSessionStateResponse result = await _settlement.GetSessionStateAsync(request, cancellationToken: cancellationToken);

            if (result.SessionState is null || result.SessionState.IsEmpty)
            {
                return null;
            }

            return new BinaryData(result.SessionState.Memory);
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task SetSessionStateAsync(
            BinaryData? sessionState,
            CancellationToken cancellationToken = default)

        {
            var request = new SetSessionStateRequest()
            {
                SessionId = _sessionId,
                SessionState = sessionState is null ? ByteString.Empty : ByteString.CopyFrom(sessionState.ToMemory().Span),
            };

            await _settlement.SetSessionStateAsync(request, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task ReleaseSession(
            CancellationToken cancellationToken = default)
        {
            var request = new ReleaseSessionRequest()
            {
                SessionId = _sessionId,
            };

            await _settlement.ReleaseSessionAsync(request, cancellationToken: cancellationToken);
        }

        ///<inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task RenewSessionLockAsync(
            CancellationToken cancellationToken = default)
        {
            var request = new RenewSessionLockRequest()
            {
                SessionId = _sessionId,
            };

            var result = await _settlement.RenewSessionLockAsync(request, cancellationToken: cancellationToken);
            SessionLockedUntil = result.LockedUntil.ToDateTimeOffset();
        }
    }
}
