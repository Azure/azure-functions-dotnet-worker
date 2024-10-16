// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

public class SkipDefaultFunctionMetadataProviderAggregator : IFunctionMetadataProviderAggregator
{
    private readonly IFunctionMetadataProvider[] _functionMetadataProviders;

    public SkipDefaultFunctionMetadataProviderAggregator(IEnumerable<IFunctionMetadataProvider> functionMetadataProviders)
    {
        _functionMetadataProviders = functionMetadataProviders
            .Where(p => p.GetType().GetCustomAttribute<DefaultMetadataProviderAttribute>() == default)
            .ToArray();
    }

    public async ValueTask<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string scriptRoot)
    {
        var aggregatedMetadata = new List<IFunctionMetadata>(_functionMetadataProviders.Length);
        foreach (var functionMetadataProvider in _functionMetadataProviders)
        {
            var functionMetadata = await functionMetadataProvider.GetFunctionMetadataAsync(scriptRoot).ConfigureAwait(false);
            aggregatedMetadata.AddRange(functionMetadata);
        }
        return aggregatedMetadata.ToImmutableArray();
    }
}
