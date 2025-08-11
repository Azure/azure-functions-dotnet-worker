// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

/// <summary>
/// Provides a composite implementation of <see cref="IFunctionMetadataProvider"/> that applies a set of transformers to function metadata.
/// </summary>
public class CompositeFunctionMetadataProvider(
    IFunctionMetadataProvider inner,
    IEnumerable<IFunctionMetadataTransformer> transformers,
    ILogger<CompositeFunctionMetadataProvider> logger) : IFunctionMetadataProvider
{
    private readonly IFunctionMetadataProvider _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly IEnumerable<IFunctionMetadataTransformer> _transformers = transformers;
    private readonly ILogger<CompositeFunctionMetadataProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
    {
        // Get the core metadata array
        var source = await _inner.GetFunctionMetadataAsync(directory);

        if (!_transformers.Any() || source.IsDefaultOrEmpty)
        {
            return source.IsDefault ? ImmutableArray<IFunctionMetadata>.Empty : source;
        }

        var builder = ImmutableArray.CreateBuilder<IFunctionMetadata>(source.Length);

        // For each function, run it through the transformer pipeline:
        // We could also call .Transform directly on the original metadata array and have the transformers
        // loop through each function on their own.
        foreach (var original in source)
        {
            IFunctionMetadata current = original;

            if (current is null)
            {
                _logger?.LogWarning("Function metadata is null for a function in {Directory}.", directory);
                continue;
            }

            foreach (var transformer in _transformers)
            {
                try
                {
                    _logger?.LogTrace("Applying {Transformer} to {FunctionName}.", transformer.GetType().Name, current?.Name ?? "<unknown>");

                    current = transformer.Transform(current!)
                        ?? throw new InvalidOperationException(
                            $"Transformer {transformer.GetType().FullName} returned null");

                }
                catch (Exception ex)
                {
                    // Fail fast by default; flip to 'continue' if you prefer best-effort.
                    _logger?.LogError(ex, "Transformer {Transformer} failed for {FunctionName}.",
                        transformer.GetType().Name, current?.Name ?? "<unknown>");
                    throw;
                }
            }

            builder.Add(current);
        }

        return builder.ToImmutable();
    }
}
