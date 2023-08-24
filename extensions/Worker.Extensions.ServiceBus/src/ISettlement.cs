// Copyright (c) Jacob Viau. All rights reserved.
// Licensed under the MIT. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface ISettlement
    {
        Task<CompleteReply> CompleteAsync(string lockToken, CancellationToken cancellation = default);
    }
}