// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader
{
    internal class FunctionMetadataJsonReader
    {
        private readonly IOptions<FunctionMetadataJsonReaderOptions> _options;
        private const string FileName = "functions.metadata";

        public FunctionMetadataJsonReader(IOptions<FunctionMetadataJsonReaderOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public virtual async Task<ImmutableArray<FunctionMetadata>> ReadMetadataAsync()
        {
            string metadataFile = Path.Combine(_options.Value.FunctionMetadataFileDrectory, FileName);

            if (File.Exists(metadataFile))
            {
                using (var fs = File.OpenText(metadataFile))
                {
                    using (var js = new JsonTextReader(fs))
                    {
                        JArray functionMetadataJson = (JArray)await JToken.ReadFromAsync(js);

                        var functionList = new List<FunctionMetadata>();

                        foreach (JObject function in functionMetadataJson)
                        {
                            FunctionMetadata metadata = function.ToObject<FunctionMetadata>();

                            // We need to re-add these by going through the BindingMetadata factory
                            metadata.Bindings.Clear();

                            JArray bindingArray = (JArray)function["bindings"];
                            if (bindingArray == null || bindingArray.Count == 0)
                            {
                                throw new FormatException("At least one binding must be declared.");
                            }

                            foreach (JObject binding in bindingArray)
                            {
                                metadata.Bindings.Add(BindingMetadata.Create(binding));
                            }

                            functionList.Add(metadata);
                        }

                        return functionList.ToImmutableArray();
                    }
                }
            }
            else
            {
                return ImmutableArray<FunctionMetadata>.Empty;
            }
        }
    }
}
