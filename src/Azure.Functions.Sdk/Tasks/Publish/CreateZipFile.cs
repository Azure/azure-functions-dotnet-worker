// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Compression;
using Microsoft.Build.Framework;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Azure.Functions.Sdk.Tasks.Publish;

/// <summary>
/// Creates a zip file for a folder.
/// </summary>
public sealed class CreateZipFile : Microsoft.Build.Utilities.Task
{
    /***
    * This class does not use IFileSystem abstraction because it does not support ZipArchive creation.
    * The improved testability is not worth the effort of adding ZipArchive support to IFileSystem.
    * Especially because ZipFile.CreateFromDirectory() includes so much edge-case handling that would
    * need to be re-implemented.
    ***/

    internal const string WorkerRootReplacement = "{WorkerRoot}";

    // Unix file permissions for -rwxrwxrwx
    internal static readonly int UnixExecutablePermissions = Convert.ToInt32("777", 8);

    [Required]
    public string SourceFolder { get; set; } = string.Empty;

    [Required]
    public string ZipName { get; set; } = string.Empty;

    [Required]
    public string PublishIntermediateTempPath { get; set; } = string.Empty;

    [Required]
    public string Executable { get; set; } = string.Empty;

    [Output]
    public string CreatedZipPath { get; private set; } = string.Empty;

    public override bool Execute()
    {
        if (!Path.IsPathRooted(SourceFolder))
        {
            Log.LogError(Strings.Zip_PathNotRooted, SourceFolder, nameof(SourceFolder));
            return false;
        }

        if (!Path.IsPathRooted(PublishIntermediateTempPath))
        {
            Log.LogError(Strings.Zip_PathNotRooted, PublishIntermediateTempPath, nameof(PublishIntermediateTempPath));
            return false;
        }

        CreatedZipPath = CreateZipFileFromDirectory();
        return true;
    }

    internal static void ModifyUnixFilePermissions(string zipFilePath, string entryName)
    {
        using ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Update);
        ZipArchiveEntry? entry = archive.GetEntry(entryName) ?? archive.GetEntry(entryName + ".exe");
        entry?.SetUnixFilePermissions(UnixExecutablePermissions);
    }

    internal string CreateZipFileFromDirectory()
    {
        string zipFileName = ZipName + ".zip";
        string destination = Path.Combine(PublishIntermediateTempPath, zipFileName);
        ZipFile.CreateFromDirectory(SourceFolder, destination);

        // Is the executable part of the zip file itself? If so, ensure it is marked executable on unix.
        if (Executable.StartsWith(WorkerRootReplacement, StringComparison.OrdinalIgnoreCase))
        {
            string executable = Executable[WorkerRootReplacement.Length..];
            if (!string.IsNullOrEmpty(executable))
            {
                ModifyUnixFilePermissions(destination, executable);
            }
        }

        return destination;
    }
}
