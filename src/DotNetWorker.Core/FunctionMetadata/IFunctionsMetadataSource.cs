// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// A source for providing metadata.
    /// </summary>
    public interface IFunctionMetadataSource
    {
        /// <summary>
        /// Gets the function metadata.
        /// </summary>
        IReadOnlyList<IFunctionMetadata> Metadata { get; }
    }
}
