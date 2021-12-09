// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader
{
    internal class JsonFunctionProvider : IFunctionProvider
    {
        private readonly Dictionary<string, IEnumerable<string>> _functionErrors = new Dictionary<string, IEnumerable<string>>();
        private readonly FunctionMetadataJsonReader _reader;

        public JsonFunctionProvider(FunctionMetadataJsonReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors =>
            _functionErrors.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray());

        public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
        {
            return _reader.ReadMetadataAsync();
        }
    }
}
