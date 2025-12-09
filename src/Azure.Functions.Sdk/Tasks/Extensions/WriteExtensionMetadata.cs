// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;
using NuGet.ProjectModel;

namespace Azure.Functions.Sdk.Tasks.Extensions;

public partial class WriteExtensionMetadata(IFileSystem fileSystem)
    : Microsoft.Build.Utilities.Task, ICancelableTask, IDisposable
{
    private static readonly HashSet<string> ExcludedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        // We know these assemblies have no triggers to include.
        "Microsoft.Azure.WebJobs.Extensions.dll",
        "Microsoft.Azure.WebJobs.Extensions.Http.dll",
    };

    private readonly CancellationTokenSource _cts = new();
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);

    public WriteExtensionMetadata()
        : this(new FileSystem())
    {
    }

    [Required]
    public string OutputPath { get; set; } = string.Empty;

    [Required]
    public ITaskItem[] ExtensionReferences { get; set; } = [];

    private string HashOutputPath => $"{OutputPath}.hash";

    public void Cancel()
    {
        _cts.Cancel();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }

    public override bool Execute()
    {
        try
        {
            ExecuteCore();
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
        }

        return !Log.HasLoggedErrors;
    }

    private void ExecuteCore()
    {
        List<ITaskItem> assemblies = [.. GetAssembliesToScan()];
        string hash = CalculateHash(assemblies);
        if (!ShouldRun(hash))
        {
            return;
        }

        List<WebJobsReference> references = [];
        MSBuildNugetLogger logger = new(Log);
        FunctionsAssemblyScanner scanner = FunctionsAssemblyScanner.FromTaskItems(ExtensionReferences);
        foreach (ITaskItem item in assemblies)
        {
            _cts.Token.ThrowIfCancellationRequested();

            try
            {
                references.AddRange(scanner.GetWebJobsReferences(item.ItemSpec, logger));
            }
            catch (Exception)
            {
                Log.LogError(Strings.ExtensionMetadata_ScanningError, item.ItemSpec);
                throw;
            }
        }

        WebJobsExtensions extensions = new(references);
        string json = JsonSerializer.Serialize(extensions, WebJobsContext.Default.WebJobsExtensions);
        _cts.Token.ThrowIfCancellationRequested();
        _fileSystem.File.WriteAllText(OutputPath, json);
        _fileSystem.File.WriteAllText(HashOutputPath, hash);
        Log.LogMessage(MessageImportance.Low, Strings.ExtensionMetadata_FinishedGenerating, hash);
    }

    /// <summary>
    /// Calculates the hash of the current set of extension assemblies. This is used to determine
    /// if the extension metadata needs to be regenerated.
    /// </summary>
    /// <returns>The calculated hash.</returns>
    /// <remarks>
    /// The hash calculation uses the following as input:
    /// - The tooling generating the hash itself (so any change to tooling forces a project update).
    /// - The list of extension assemblies (path, write time).
    ///
    /// We use this method instead of MSBuild inputs/outputs to ensure we re-run when the total set of
    /// assemblies changes, not just when individual files change.
    /// </remarks>
    private string CalculateHash(List<ITaskItem> assemblies)
    {
        using FnvHash64Function algorithm = new();
        using HashObjectWriter hash = new(algorithm);

        // Verify if the tooling has changed.
        hash.WriteNameValue("version", ThisAssembly.Version.ToString());
        hash.WriteNameValue("moduleId", ThisAssembly.ModuleVersionId);

        hash.WriteArrayStart("assemblies");
        foreach (ITaskItem item in assemblies)
        {
            hash.WriteObjectStart();
            hash.WriteNameValue("path", item.ItemSpec);
            hash.WriteNameValue("lastWriteTimeUtc", _fileSystem.File.GetLastWriteTimeUtc(item.ItemSpec).ToString());
            hash.WriteObjectEnd();
        }

        hash.WriteArrayEnd();
        return hash.GetHash();
    }

    private bool ShouldRun(string hash)
    {
        if (!_fileSystem.File.Exists(HashOutputPath)
            || !_fileSystem.File.Exists(OutputPath))
        {
            Log.LogMessage(
                MessageImportance.Low,
                Strings.ExtensionMetadata_DoesNotExist);
            return true;
        }

        string existingHash = _fileSystem.File.ReadAllText(HashOutputPath);
        if (existingHash != hash)
        {
            Log.LogMessage(
                MessageImportance.Low,
                Strings.ExtensionMetadata_HashOutOfDate,
                existingHash,
                hash);
            return true;
        }
        else
        {
            Log.LogMessage(
                MessageImportance.Low,
                Strings.ExtensionMetadata_HashUpToDate,
                existingHash);
            return false;
        }
    }

    private bool ShouldScanExtensionAssembly(ITaskItem item)
    {
        string fileName = _fileSystem.Path.GetFileName(item.ItemSpec);
        return item.GetMetadata("FrameworkReferenceName") == string.Empty // framework references are not scanned.
            && !ExcludedAssemblies.Contains(fileName) // exclude known assemblies
            && (!item.TryGetNuGetPackageId(out string? packageId) // if it has a NugetPackageId, is it excluded?
                || FunctionsAssemblyScanner.ShouldScanPackage(packageId));
    }

    private IEnumerable<ITaskItem> GetAssembliesToScan()
    {
        foreach (ITaskItem item in ExtensionReferences)
        {
            if (!ShouldScanExtensionAssembly(item))
            {
                Log.LogMessage(
                    MessageImportance.Low,
                    Strings.ExtensionMetadata_SkippingExcludedAssembly,
                    item.ItemSpec);
                continue;
            }

            yield return item;
        }
    }

    private class WebJobsExtensions(List<WebJobsReference> extensions)
    {
        public List<WebJobsReference> Extensions => extensions;
    }

    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Serialization,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true)]
    [JsonSerializable(typeof(WebJobsExtensions))]
    private partial class WebJobsContext : JsonSerializerContext
    {
    }
}
