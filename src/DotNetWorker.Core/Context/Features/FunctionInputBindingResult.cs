// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// A type representing the result of binding function inputs.
    /// </summary>
    public class FunctionInputBindingResult
    {
        /// <summary>
        /// Creates a new instance of <see cref="FunctionInputBindingResult"/>
        /// </summary>
        /// <param name="values">An array of function input values.</param>
        public FunctionInputBindingResult(object?[] values)
        {
            Values = values;
        }

        /// <summary>
        /// Gets the values of bound function inputs.
        /// </summary>
        public object?[] Values { get; }
    }
}
