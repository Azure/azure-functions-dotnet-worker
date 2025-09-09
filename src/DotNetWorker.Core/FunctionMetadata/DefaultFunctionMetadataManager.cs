// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    internal sealed class DefaultFunctionMetadataManager : IFunctionMetadataManager
    {
        private readonly IFunctionMetadataProvider _functionMetadataProvider;
        private readonly ImmutableArray<IFunctionMetadataTransformer> _transformers;
        private readonly ILogger<DefaultFunctionMetadataManager> _logger;

        public DefaultFunctionMetadataManager(IFunctionMetadataProvider functionMetadataProvider,
                                              IEnumerable<IFunctionMetadataTransformer> transformers,
                                              ILogger<DefaultFunctionMetadataManager> logger)
        {
            _functionMetadataProvider = functionMetadataProvider;
            _transformers = transformers.ToImmutableArray();
            _logger = logger;
        }

        public async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            ImmutableArray<IFunctionMetadata> functionMetadata = await _functionMetadataProvider.GetFunctionMetadataAsync(directory);

            return ApplyTransforms(functionMetadata);
        }

        private ImmutableArray<IFunctionMetadata> ApplyTransforms(ImmutableArray<IFunctionMetadata> functionMetadata)
        {
            // Return early if there are no transformers to apply
            if (_transformers.Length == 0)
            {
                return functionMetadata;
            }

            var metadataResult = functionMetadata.ToBuilder();

            foreach (var transformer in _transformers)
            {
                try
                {
                    _logger?.LogTrace("Applying metadata transformer: {Transformer}.", transformer.Name);
                    transformer.Transform(metadataResult);
                }
                catch (Exception exc)
                {
                    _logger?.LogError(exc, "Metadata transformer '{Transformer}' failed.", transformer.Name);
                    throw;
                }
            }

            return metadataResult.ToImmutable();
        }
    }
}
