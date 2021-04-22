// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Microsoft.NET.Sdk.Functions.MSBuild.Tasks
{
#if NET472
    [LoadInSeparateAppDomain]
    public class CreateZipFileTask : AppDomainIsolatedTask
#else
    public class CreateZipFileTask : Task
#endif
    {
        [Required]
        public string? FolderToZip { get; set; }

        [Required]
        public string? ProjectName { get; set; }

        [Required]
        public string? PublishIntermediateTempPath { get; set; }

        [Output]
        public string? CreatedZipPath { get; private set; }

        public override bool Execute()
        {
            string zipFileName = ProjectName + " - " + DateTime.Now.ToString("yyyyMMddHHmmssFFF") + ".zip";
            CreatedZipPath = Path.Combine(PublishIntermediateTempPath, zipFileName);
            ZipFile.CreateFromDirectory(FolderToZip, CreatedZipPath);
            return true;
        }
    }
}
