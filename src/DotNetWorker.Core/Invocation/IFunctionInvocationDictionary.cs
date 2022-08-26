// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    /// <summary>
    /// Keeps track of inflight invocations.
    /// </summary>
    internal interface IFunctionInvocationDictionary
    {
        /// <summary>
        /// Attempts to store the specified key and value, where 'invocationId' is the key.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        /// <param name="details"><see cref="FunctionInvocationDetails"/></param>
        /// <returns><see cref="bool"/> representing operation success</returns>
        bool TryAddInvocationDetails(string invocationId, FunctionInvocationDetails details);

        /// <summary>
        /// Attempts to remove the specified key.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        /// <returns><see cref="bool"/> representing operation success</returns>
        bool TryRemoveInvocationDetails(string invocationId);

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="invocationId">Invocation ID</param>
        /// <param name="details">Out parameter for <see cref="FunctionInvocationDetails"/></param>
        /// <returns>The <see cref="FunctionInvocationDetails"/>.</returns>
        /// <returns><see cref="bool"/> representing operation success</returns>
        bool TryGetInvocationDetails(string invocationId, out FunctionInvocationDetails details);
    }
}
