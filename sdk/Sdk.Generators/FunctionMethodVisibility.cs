// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// Represents the visibility of an "azure function" method and its parent classes.
    /// </summary>
    internal enum FunctionMethodVisibility
    {
        /// <summary>
        /// The method and it's parent classes are public & visible.
        /// </summary>
        Public,

        /// <summary>
        /// The method is public, but one or more of its parent classes are not public.
        /// </summary>
        PublicButContainingTypeNotVisible,

        /// <summary>
        /// The method is not public.
        /// </summary>
        NotPublic
    }
}
