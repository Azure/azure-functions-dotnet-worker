// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Azure.Functions.Worker.Sdk.Tasks
{
    public class EnhanceExtensionsMetadata : Task
    {
        [Required]
        public string? ExtensionsJsonPath { get; set; }

        [Required]
        public string? OutputPath { get; set; }

        public override bool Execute()
        {
            string json = File.ReadAllText(ExtensionsJsonPath);

            var extensionsMetadata = JsonSerializer.Deserialize<ExtensionsMetadata>(json);
            ExtensionsMetadataEnhancer.AddHintPath(extensionsMetadata?.Extensions ?? Enumerable.Empty<ExtensionReference>());

            string newJson = JsonSerializer.Serialize(extensionsMetadata);
            File.WriteAllText(OutputPath, newJson);

            File.Delete(ExtensionsJsonPath);

            return true;
        }
    }
}
