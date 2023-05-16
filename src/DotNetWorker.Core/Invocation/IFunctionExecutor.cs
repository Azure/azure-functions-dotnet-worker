// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    /// <summary>
    /// Provides a mechanism to execute function code.
    /// </summary>
    public interface IFunctionExecutor
    {
        /// <summary>
        /// Asynchronously executes the function code.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/> instance.</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        ValueTask ExecuteAsync(FunctionContext context);
    }
}
