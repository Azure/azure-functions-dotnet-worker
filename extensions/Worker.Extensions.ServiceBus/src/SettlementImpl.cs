// Copyright (c) Jacob Viau. All rights reserved.
// Licensed under the MIT. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    internal class SettlementImpl : ISettlement
    {
        private readonly ILogger _logger;
        private readonly Settlement.SettlementClient _client;

        public SettlementImpl(Settlement.SettlementClient client, ILogger<SettlementImpl> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<CompleteReply> CompleteAsync(string lockToken, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Completing message with lock token {LockToken}", lockToken);
            CompleteReply reply = await _client.CompleteAsync(new() { Locktoken = lockToken}, cancellationToken: cancellation);
            return reply;
        }
    }
}