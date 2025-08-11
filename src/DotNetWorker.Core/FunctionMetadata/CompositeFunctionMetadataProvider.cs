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
        var source = await _inner.GetFunctionMetadataAsync(directory);

        if (!_transformers.Any() || source.IsDefaultOrEmpty)
        {
            return source.IsDefault ? ImmutableArray<IFunctionMetadata>.Empty : source;
        }

        foreach (var transformer in _transformers)
        {
            try
            {
                _logger?.LogTrace("Applying {Transformer} to function metadata array.", transformer.GetType().Name);
                source = transformer.Transform(source);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Transformer {Transformer} failed.", transformer.GetType().Name);
                throw;
            }
        }

        return source;
    }
}
