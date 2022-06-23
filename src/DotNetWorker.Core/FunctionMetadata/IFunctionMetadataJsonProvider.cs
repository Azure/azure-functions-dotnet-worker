// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Provide function metadata information in the form of JsonElement objects
    /// </summary>
    public interface IFunctionMetadataJsonProvider
    {

        /// <summary>
        /// Gets all function metadata that this provider knows about asynchronously
        /// </summary>
        /// <returns>A Task with IEnumerable of JsonElement</returns>
        Task<ImmutableArray<JsonElement>> GetFunctionMetadataJsonAsync(string directory);
    }
}
