// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface IFunctionsApplication
    {
        void LoadFunction(FunctionDefinition definition);

        Task InvokeFunctionAsync(FunctionContext context);

        FunctionContext CreateContext(IInvocationFeatures features);

        /// <summary>
        /// Cancels an invocations associated <see cref="CancellationTokenSource"/>.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        void CancelInvocation(string invocationId);

        /// <summary>
        /// Removes a given invocation from the <see cref="FunctionInvocationManager"/>.
        /// Should be called on completed invocations.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        void RemoveInvocationRecord(string invocationId);
    }
}
