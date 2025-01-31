// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Azure.Functions.Worker.Sdk.Tasks
{
#if NET472
    [LoadInSeparateAppDomain]
    public class EnhanceExtensionsMetadata : AppDomainIsolatedTask
#else
    public class EnhanceExtensionsMetadata : Task
#endif
    {
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { WriteIndented = true };

        [Required]
        public string? ExtensionsJsonPath { get; set; }

        [Required]
        public string? OutputPath { get; set; }

        public ITaskItem[]? AdditionalExtensions { get; set; }

        public override bool Execute()
        {
            string json = File.ReadAllText(ExtensionsJsonPath);

            var extensionsMetadata = JsonSerializer.Deserialize<ExtensionsMetadata>(json) ?? new ExtensionsMetadata();
            ExtensionsMetadataEnhancer.AddHintPath(extensionsMetadata.Extensions);

            foreach (ITaskItem item in AdditionalExtensions ?? Enumerable.Empty<ITaskItem>())
            {
                extensionsMetadata.Extensions.AddRange(ExtensionsMetadataEnhancer.GetWebJobsExtensions(item.ItemSpec));
            }

            string newJson = JsonSerializer.Serialize(extensionsMetadata, _serializerOptions);
            File.WriteAllText(OutputPath, newJson);

            return true;
        }
    }
}
