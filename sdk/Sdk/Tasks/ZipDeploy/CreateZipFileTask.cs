// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Json;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
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
        internal const string WorkerRootReplacement = "{WorkerRoot}";

        // Unix file permissions for -rwxrwxrwx
        internal static readonly int UnixExecutablePermissions = Convert.ToInt32("100777", 8) << 16;

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
            if (FolderToZip == null)
            {
                return false;
            }

            string zipFileName = ProjectName + " - " + DateTime.Now.ToString("yyyyMMddHHmmssFFF") + ".zip";
            CreatedZipPath = Path.Combine(PublishIntermediateTempPath, zipFileName);

            CreateZipFileFromDirectory(FolderToZip, CreatedZipPath);

            return true;
        }

        internal static void CreateZipFileFromDirectory(string directoryToZip, string destinationArchiveFileName)
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(directoryToZip, destinationArchiveFileName);

            // We may need to modify permissions for Linux.
            string configJson = File.ReadAllText(Path.Combine(directoryToZip, "worker.config.json"));
            var configJsonDoc = JsonDocument.Parse(configJson);

            if (configJsonDoc.RootElement.TryGetProperty("description", out JsonElement description) &&
                description.TryGetProperty("defaultExecutablePath", out JsonElement defaultExecutablePath) &&
                defaultExecutablePath.ValueKind == JsonValueKind.String)
            {
                string? executable = defaultExecutablePath.GetString();

                // if the executable path contains "{WorkerRoot}", it means we'll be executing this file directly (as 
                // opposed to running with 'dotnet ...'). If so, we need to make the file executable for Linux.
                if (executable != null && executable.Contains(WorkerRootReplacement))
                {
                    executable = executable.Replace(WorkerRootReplacement, string.Empty);

                    if (!string.IsNullOrEmpty(executable))
                    {
                        ModifyUnixFilePermissions(destinationArchiveFileName, directoryToZip, executable);
                    }
                }
            }
        }

        internal static void ModifyUnixFilePermissions(string zipFilePath, string entryRootPath, string entryName)
        {
            using (var zipFile = new ZipFile(zipFilePath))
            {
                zipFile.EntryFactory = new EntryFactory();
                zipFile.BeginUpdate();

                try
                {
                    string entryFullPath = Path.Combine(entryRootPath, entryName);

                    // In Windows, we leave off the .exe, so need to adjust the entry name
                    if (!File.Exists(entryFullPath))
                    {
                        entryFullPath += ".exe";
                        entryName += ".exe";

                        if (!File.Exists(entryFullPath))
                        {
                            return;
                        }
                    }

                    // This will overwrite the existing entry.
                    zipFile.Add(entryFullPath, entryName);
                }
                finally
                {
                    zipFile.CommitUpdate();
                    zipFile.Close();
                }
            }
        }

        private class EntryFactory : IEntryFactory
        {
            private IEntryFactory _internal = new ZipEntryFactory();

            public INameTransform NameTransform { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ZipEntryFactory.TimeSetting Setting => throw new NotImplementedException();

            public DateTime FixedDateTime => throw new NotImplementedException();

            public ZipEntry MakeDirectoryEntry(string directoryName)
            {
                throw new NotImplementedException();
            }

            public ZipEntry MakeDirectoryEntry(string directoryName, bool useFileSystem)
            {
                throw new NotImplementedException();
            }

            public ZipEntry MakeFileEntry(string fileName)
            {
                throw new NotImplementedException();
            }

            public ZipEntry MakeFileEntry(string fileName, bool useFileSystem)
            {
                throw new NotImplementedException();
            }

            public ZipEntry MakeFileEntry(string fileName, string entryName, bool useFileSystem)
            {
                var zipEntry = _internal.MakeFileEntry(fileName, entryName, useFileSystem);

                // Unix
                zipEntry.HostSystem = 3;

                // Unix file permissions for -rwxrwxrwx
                zipEntry.ExternalFileAttributes = UnixExecutablePermissions;

                return zipEntry;
            }
        }
    }
}
