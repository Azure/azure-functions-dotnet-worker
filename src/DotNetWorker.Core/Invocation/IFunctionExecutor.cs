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
        /// Executes the function code.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/> instance.</param>
        /// <returns>A <see cref="Task"/> representing the result of execute operation.</returns>
        ValueTask ExecuteAsync(FunctionContext context);
    }
}
