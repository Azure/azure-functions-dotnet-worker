// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Manages function metadata, providing functionality that combines metadata from the registered provider and metadata transforms.
    /// </summary>
    public interface IFunctionMetadataManager
    {
        /// <summary>
        /// Retrieves all function metadata for the current application.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous metadata retrieval operation, where the result is an <see cref="ImmutableArray{IFunctionMetadata}"/>.</returns>
        Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory);
    }
}
