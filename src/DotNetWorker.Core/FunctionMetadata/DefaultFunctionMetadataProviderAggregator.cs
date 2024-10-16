// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

public class DefaultFunctionMetadataProviderAggregator : IFunctionMetadataProviderAggregator
{
    private readonly IEnumerable<IFunctionMetadataProvider> _functionMetadataProviders;

    public DefaultFunctionMetadataProviderAggregator(IEnumerable<IFunctionMetadataProvider> functionMetadataProviders)
    {
        _functionMetadataProviders = functionMetadataProviders;
    }

    public async ValueTask<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string scriptRoot)
    {
        var aggregatedMetadata = new List<IFunctionMetadata>();
        foreach (var functionMetadataProvider in _functionMetadataProviders)
        {
            var functionMetadata = await functionMetadataProvider.GetFunctionMetadataAsync(scriptRoot).ConfigureAwait(false);
            aggregatedMetadata.AddRange(functionMetadata);
        }
        return aggregatedMetadata.ToImmutableArray();
    }
}
