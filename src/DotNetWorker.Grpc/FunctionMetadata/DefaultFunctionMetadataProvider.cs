// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultFunctionMetadataProvider : IFunctionMetadataProvider
    {
        private const string FileName = "functions.metadata";

        private readonly IEnumerable<IFunctionMetadataSource> _sources;
        private readonly WorkerOptions _options;
        private readonly JsonSerializerOptions _deserializationOptions = new() { PropertyNameCaseInsensitive = true };

        public DefaultFunctionMetadataProvider(IEnumerable<IFunctionMetadataSource> sources, IOptions<WorkerOptions> options)
        {
            _sources = sources;
            _options = options.Value;
        }

        public virtual async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            ImmutableArray<IFunctionMetadata>.Builder builder = ImmutableArray.CreateBuilder<IFunctionMetadata>();
            builder.AddRange(await GetDefaultFunctionMetadataAsync(directory));

            foreach (var source in _sources)
            {
                builder.AddRange(source.Metadata);
            }

            return builder.ToImmutable();
        }

        internal static JsonElement GetRawBindings(JsonElement jsonMetadata)
        {
            jsonMetadata.TryGetProperty("bindings", out JsonElement bindingsJson);

            if(bindingsJson.GetArrayLength() == 0)
            {
                var funcName = jsonMetadata.GetProperty("name");
                throw new FormatException($"At least one binding must be declared in function `{funcName}`");
            }

            return bindingsJson;
        }

        private async Task<IEnumerable<IFunctionMetadata>> GetDefaultFunctionMetadataAsync(string directory)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (_options.Internal.DisableDefaultFunctionMetadata)
            {
                return Enumerable.Empty<IFunctionMetadata>();
            }
#pragma warning restore CS0618 // Type or member is obsolete

            string metadataFile = Path.Combine(directory, FileName);
            if (!File.Exists(metadataFile))
            {
                throw new FileNotFoundException($"Function metadata file not found. File path used:{metadataFile}");
            }

            using var fs = File.OpenRead(metadataFile);
            // deserialize as json element to preserve raw bindings
            var jsonMetadataList = await JsonSerializer.DeserializeAsync<JsonElement>(fs);
            return ParseMetadata(jsonMetadataList);
        }

        private IEnumerable<IFunctionMetadata> ParseMetadata(JsonElement json)
        {
            foreach (var jsonMetadata in json.EnumerateArray())
            {
                var functionMetadata = JsonSerializer.Deserialize<RpcFunctionMetadata>(jsonMetadata.GetRawText(), _deserializationOptions)
                    ?? throw new NullReferenceException("Function metadata could not be found.");

                // hard-coded values that are checked for when the host validates functions
                functionMetadata.IsProxy = false;
                functionMetadata.Language = "dotnet-isolated";
                functionMetadata.FunctionId = Guid.NewGuid().ToString();

                var rawBindings = GetRawBindings(jsonMetadata);

                foreach (var binding in rawBindings.EnumerateArray())
                {
                    functionMetadata.RawBindings.Add(binding.GetRawText());
                }

                yield return functionMetadata;
            }
        }
    }
}
