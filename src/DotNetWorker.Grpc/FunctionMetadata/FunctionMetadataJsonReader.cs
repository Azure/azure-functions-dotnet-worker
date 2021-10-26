using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.Functions.Worker
{ 
    internal class FunctionMetadataJsonReader
    {
        private string _directory;
        private const string FileName = "functions.metadata";

        public FunctionMetadataJsonReader(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public virtual async Task<ImmutableArray<FunctionMetadata>> ReadMetadataAsync()
        {
            string metadataFile = Path.Combine(_directory, FileName);

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
                                metadata.Bindings.Add(WebJobs.Script.Description.BindingMetadata.Create(binding));
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
