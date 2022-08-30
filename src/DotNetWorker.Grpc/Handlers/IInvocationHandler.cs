// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Handlers
{
    internal interface IInvocationHandler
    {
        /// <summary>
        /// Invokes a function based on incoming <see cref="InvocationRequest"/> and returns
        /// an <see cref="InvocationResponse"/>. This includes creating and keeping track of
        /// an associated cancellation token source for the invocation.
        /// </summary>
        /// <param name="request">Function invocation request</param>
        /// <returns><see cref="InvocationResponse"/></returns>
        Task<InvocationResponse> InvokeAsync(InvocationRequest request);

        /// <summary>
        /// Cancels an invocation's associated <see cref="CancellationTokenSource"/>.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        void Cancel(string invocationId);
    }
}
