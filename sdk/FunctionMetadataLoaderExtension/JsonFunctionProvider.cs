﻿using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader
{
    internal class JsonFunctionProvider : IFunctionProvider
    {
        private readonly FunctionMetadataJsonReader _reader;

        public JsonFunctionProvider(FunctionMetadataJsonReader reader, string metadataFileDirectory)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors { get; }

        public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
        {
            return _reader.ReadMetadataAsync();
        }
    }
}