// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Specifies the direction of the binding.
    /// </summary>
    public enum BindingDirection
    {
        /// <summary>
        /// Identifies an input binding; a binding that provides data to the function.
        /// </summary>
        In,
        /// <summary>
        /// Identifies an output binding; a binding that receives data from the function.
        /// </summary>
        Out
    };
}
