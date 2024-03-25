// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

/// <summary>
/// Aggregates all the function metadata.
/// </summary>
public interface IFunctionMetadataProviderAggregator
{
    /// <summary>
    /// Gets the aggregated function metadata that this aggregator collected from the providers asynchronously
    /// </summary>
    /// <returns>A Task with IEnumerable of FunctionMetadata</returns>
    ValueTask<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string scriptRoot);
}
