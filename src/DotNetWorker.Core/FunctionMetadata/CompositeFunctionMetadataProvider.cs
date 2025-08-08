// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

/// <summary>
/// Provides a composite implementation of <see cref="IFunctionMetadataProvider"/> that applies a set of transformers to function metadata.
/// </summary>
public class CompositeFunctionMetadataProvider(
    IFunctionMetadataProvider inner,
    IServiceProvider services) : IFunctionMetadataProvider
{
    private readonly IFunctionMetadataProvider _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly IEnumerable<IFunctionMetadataTransformer> _transformers = services
            .GetServices<IFunctionMetadataTransformer>()
            .ToImmutableArray();

    /// <inheritdoc/>
    public async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
    {
        // Get the core metadata array
        var originals = await _inner.GetFunctionMetadataAsync(directory);


        var builder = ImmutableArray.CreateBuilder<IFunctionMetadata>(originals.Length);

        // For each function, run it through the transformer pipeline:
        // We could also call .Transform directly on the original metadata array and have the transformers
        // loop through each function on their own.
        foreach (var m in originals)
        {
            IFunctionMetadata current = m;
            foreach (var tx in _transformers)
            {
                current = tx.Transform(current)
                    ?? throw new InvalidOperationException(
                        $"Transformer {tx.GetType().FullName} returned null");
            }
            builder.Add(current);
        }

        return builder.ToImmutable();
    }
}
