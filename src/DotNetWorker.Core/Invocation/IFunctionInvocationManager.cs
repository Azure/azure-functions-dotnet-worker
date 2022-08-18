// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    interface IFunctionInvocationManager
    {
        /// <summary>
        /// Attempts to store the specified key and value, where 'invocationId' is the key.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        /// <param name="details"><see cref="FunctionInvocationDetails"/></param>
        internal void TryAddInvocationDetails(string invocationId, FunctionInvocationDetails details);

        /// <summary>
        /// Attempts to remove the specified key.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        internal void TryRemoveInvocationDetails(string invocationId);

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        /// <returns>The <see cref="FunctionInvocationDetails"/>.</returns>
        internal FunctionInvocationDetails? TryGetInvocationDetails(string invocationId);
    }
}
