// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// The direction that data will flow to or from your function
    /// </summary>
    public enum BindingDirection
    {
        /// <summary>
        /// Indiates that binding data is coming into the function
        /// </summary>
        In,
        /// <summary>
        /// Indicates that binding data is set by the function
        /// </summary>
        Out
    };
}
