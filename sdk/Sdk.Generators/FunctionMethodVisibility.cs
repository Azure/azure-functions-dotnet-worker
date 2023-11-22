// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// Represents the visibility of a "azure function" method and it's parent classes.
    /// </summary>
    internal enum FunctionMethodVisibility
    {
        /// <summary>
        /// The method and it's parent classes are public.
        /// </summary>
        PublicAndVisible,

        /// <summary>
        /// The method is public, but one or more of it's parent classes are not public.
        /// </summary>
        PublicButContainingTypeNotVisible,

        /// <summary>
        /// The method is not public.
        /// </summary>
        NotPublic
    }
}
