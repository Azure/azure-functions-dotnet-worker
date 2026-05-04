// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tasks;

/// <summary>
/// Generates a worker config file for dotnet-isolated functions.
/// </summary>
/// <param name="fileSystem">The file system to write the file to.</param>
public partial class GenerateWorkerConfig(IFileSystem fileSystem)
    : Microsoft.Build.Utilities.Task
{
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateWorkerConfig"/> class.
    /// </summary>
    public GenerateWorkerConfig()
        : this(new FileSystem())
    {
    }

    /// <summary>
    /// Gets or sets the app's executable path.
    /// </summary>
    /// <remarks>
    /// This is typically one of the following:
    /// 1. netcoreapp: the dotnet executable
    /// 2. netcoreapp self-contained: the app's executable
    /// 3. netfx: the app's executable
    /// </remarks>
    [Required]
    public string Executable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the app's entry point.
    /// </summary>
    /// <remarks>
    /// This is typically the app's dll, which is passed to the dotnet executable.
    /// </remarks>
    [Required]
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to write the config file to.
    /// </summary>
    /// <remarks>
    /// It is the callers responsibility to ensure the directory exists.
    /// </remarks>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <inheritdoc />
    public override bool Execute()
    {
        Config config = new(Executable, EntryPoint);
        string json = JsonSerializer.Serialize(config, WorkerConfigContext.Default.Config);
        _fileSystem.File.WriteAllText(OutputPath, json);
        return true;
    }

    private class Config(string executable, string entry)
    {
        public Description Description { get; } = new(executable, entry);
    }

    private class Description(string executable, string entry)
    {
        public string Language => "dotnet-isolated";

        public IEnumerable<string> Extensions => [".dll"];

        public string DefaultExecutablePath => executable;

        public string DefaultWorkerPath => entry;

        public string WorkerIndexing => "true";

        public bool CanUsePlaceholder => true;
    }

    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Serialization,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true)]
    [JsonSerializable(typeof(Config))]
    private partial class WorkerConfigContext : JsonSerializerContext;
}
