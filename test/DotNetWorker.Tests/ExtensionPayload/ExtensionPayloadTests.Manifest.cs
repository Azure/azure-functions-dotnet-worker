// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed partial class ExtensionPayloadTests
{
    private const string MetadataLoaderName = "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader";
    private const string BootstrapEntry = """
        {"name":"Startup","typeName":"Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader, Version=1.0.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c","hintPath":"./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll"}
        """;

    private const string SdkManifest = $$"""
        {
          "schemaVersion": 17,
          "custom": { "text": "<unknown>&", "numbers": [1, 1.234e-7], "flag": true, "value": null },
          "extensions": [
            {{BootstrapEntry}},
            {
              "name": "Startup",
              "typeName": "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageQueuesWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage.Queues",
              "hintPath": "./.azurefunctions/Microsoft.Azure.WebJobs.Extensions.Storage.Queues.dll",
              "unknown": { "order": [3, 2, 1] }
            },
            {
              "name": "FunctionMetadataLoaderOther",
              "typeName": "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Startup, Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.Other",
              "hintPath": "./.azurefunctions/nested/Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll"
            }
          ],
          "afterExtensions": ["preserve", { "opaque": "value" }]
        }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultSdkBootstrapIsFilteredOnlyFromZipAndOtherMetadataAndFilesArePreserved(bool withBom)
    {
        string manifestPath = Path.Combine(_root, "extensions.json");
        byte[] manifestBytes = Encoding.UTF8.GetBytes(SdkManifest);
        if (withBom)
        {
            manifestBytes = [0xef, 0xbb, 0xbf, .. manifestBytes];
        }

        File.WriteAllBytes(manifestPath, manifestBytes);
        string[] assets =
        [
            MetadataLoaderName + ".dll",
            MetadataLoaderName + ".pdb",
            "Microsoft.Azure.WebJobs.Extensions.Storage.Queues.dll",
            "Microsoft.Azure.WebJobs.Script.Abstractions.dll",
            "Newtonsoft.Json.dll",
            "function.deps.json",
            Path.Combine("fr", "Extension.resources.dll"),
            MetadataLoaderName + ".Other.dll",
            Path.Combine("nested", MetadataLoaderName + ".dll"),
            Path.Combine("nested", MetadataLoaderName + ".pdb"),
            Path.Combine("runtimes", "linux-x64", "native", "shared.so")
        ];
        foreach (string asset in assets)
        {
            string path = Path.Combine(_root, ".azurefunctions", asset);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "preserve source: " + asset);
        }

        string[] sourcePaths = [manifestPath, .. assets.Select(asset => Path.Combine(_root, ".azurefunctions", asset))];
        byte[][] sourceBytes = sourcePaths.Select(File.ReadAllBytes).ToArray();
        DateTime[] writeTimes = sourcePaths.Select(File.GetLastWriteTimeUtc).ToArray();
        using (FileStream stream = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None))
        using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
        {
            Assert.Null(archive.GetEntry(".azurefunctions/" + MetadataLoaderName + ".dll"));
            Assert.Null(archive.GetEntry(".azurefunctions/" + MetadataLoaderName + ".pdb"));
            foreach (string asset in assets.Skip(2))
            {
                string name = ".azurefunctions/" + asset.Replace(Path.DirectorySeparatorChar, '/');
                Assert.Equal(File.ReadAllBytes(Path.Combine(_root, ".azurefunctions", asset)), ReadManifestTestEntry(archive, name));
            }

            JsonNode expected = JsonNode.Parse(SdkManifest)!;
            expected["extensions"]!.AsArray().RemoveAt(0);
            JsonNode actual = JsonNode.Parse(ReadManifestTestEntry(archive, "extensions.json"))!;
            Assert.True(JsonNode.DeepEquals(expected, actual));
        }

        for (int index = 0; index < sourcePaths.Length; index++)
        {
            Assert.Equal(sourceBytes[index], File.ReadAllBytes(sourcePaths[index]));
            Assert.Equal(writeTimes[index], File.GetLastWriteTimeUtc(sourcePaths[index]));
        }

        AssertNoTemporaryArchives();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManifestWithoutBootstrapIsAByteForByteNoOpButKnownRootAssetsAreStillExcluded(bool withBom)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(" \r\n" + Manifest + "\r\n ");
        if (withBom)
        {
            bytes = [0xef, 0xbb, 0xbf, .. bytes];
        }

        string manifestPath = Path.Combine(_root, "extensions.json");
        File.WriteAllBytes(manifestPath, bytes);
        File.WriteAllText(Path.Combine(_root, ".azurefunctions", MetadataLoaderName + ".dll"), "ordinary publish DLL");
        File.WriteAllText(Path.Combine(_root, ".azurefunctions", MetadataLoaderName + ".pdb"), "ordinary publish PDB");
        using (FileStream stream = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None))
        using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
        {
            Assert.Equal(bytes, ReadManifestTestEntry(archive, "extensions.json"));
            Assert.Null(archive.GetEntry(".azurefunctions/" + MetadataLoaderName + ".dll"));
            Assert.Null(archive.GetEntry(".azurefunctions/" + MetadataLoaderName + ".pdb"));
        }

        Assert.Equal(bytes, File.ReadAllBytes(manifestPath));
        Assert.Equal("ordinary publish DLL", File.ReadAllText(Path.Combine(_root, ".azurefunctions", MetadataLoaderName + ".dll")));
        Assert.Equal("ordinary publish PDB", File.ReadAllText(Path.Combine(_root, ".azurefunctions", MetadataLoaderName + ".pdb")));
        AssertNoTemporaryArchives();
    }

    [Fact]
    public async Task EveryRequestFiltersTheCurrentManifestWithoutChangingOrdinaryPublishOutput()
    {
        ExtensionPayload producer = CreateProducer();
        string manifestPath = Path.Combine(_root, "extensions.json");
        File.WriteAllText(Path.Combine(_root, ".azurefunctions", MetadataLoaderName + ".dll"), "retained on disk");
        string[] manifests = [Manifest, SdkManifest];
        for (int index = 0; index < manifests.Length; index++)
        {
            byte[] source = Encoding.UTF8.GetBytes(manifests[index]);
            File.WriteAllBytes(manifestPath, source);
            using RecordingClient client = new();
            await producer.SendAsync("manifest-request-" + index, client, CancellationToken.None);
            Assert.Equal(StatusResult.Types.Status.Success, client.Messages.Last().ExtensionPayloadResponse.Result.Status);
            byte[] zip = client.Messages.Where(message => message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Chunk)
                .SelectMany(message => message.ExtensionPayloadResponse.Chunk.ToByteArray()).ToArray();
            using MemoryStream bytes = new(zip);
            using ZipArchive archive = new(bytes, ZipArchiveMode.Read);
            Assert.Null(archive.GetEntry(".azurefunctions/" + MetadataLoaderName + ".dll"));
            using JsonDocument document = JsonDocument.Parse(ReadManifestTestEntry(archive, "extensions.json"));
            JsonElement extensions = document.RootElement.GetProperty("extensions");
            Assert.Equal(index == 0 ? 1 : 2, extensions.GetArrayLength());
            Assert.Equal(source, File.ReadAllBytes(manifestPath));
            Assert.Equal("retained on disk", File.ReadAllText(Path.Combine(_root, ".azurefunctions", MetadataLoaderName + ".dll")));
            AssertNoTemporaryArchives();
        }
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"extensions\":{}}")]
    [InlineData("{\"extensions\":[null]}")]
    [InlineData("{\"extensions\":[],\"extensions\":[]}")]
    public async Task MalformedManifestFailsWithSafeTerminalAndCleanupWithoutChangingSource(string manifest)
    {
        byte[] source = Encoding.UTF8.GetBytes(manifest);
        File.WriteAllBytes(Path.Combine(_root, "extensions.json"), source);
        await AssertPayloadRequestFailsAsync();
        Assert.Equal(source, File.ReadAllBytes(Path.Combine(_root, "extensions.json")));
    }

    [Fact]
    public async Task OversizedManifestInputFailsWithCleanupAndUnchangedSource()
    {
        byte[] source = Encoding.UTF8.GetBytes(Manifest.PadRight(ExtensionPayload.MaximumManifestBytes + 1));
        File.WriteAllBytes(Path.Combine(_root, "extensions.json"), source);
        await AssertPayloadRequestFailsAsync();
        Assert.Equal(source, File.ReadAllBytes(Path.Combine(_root, "extensions.json")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task FilteredManifestOutputAccepts1MiBAndRejectsOneByteOverEvenWhenInputFits(int excess)
    {
        int expectedLength = ExtensionPayload.MaximumManifestBytes + excess;
        const int EscapedCharacters = 128;
        string escaped = string.Concat(Enumerable.Repeat("\\u003C", EscapedCharacters));
        int serializedBaseLength = Encoding.UTF8.GetByteCount("{\"extensions\":[],\"note\":\"" + escaped + "\"}");
        string sourceManifest = "{\"extensions\":[" + BootstrapEntry + "],\"note\":\"" + new string('<', EscapedCharacters)
            + new string('a', expectedLength - serializedBaseLength) + "\"}";
        byte[] source = Encoding.UTF8.GetBytes(sourceManifest);
        Assert.True(source.Length <= ExtensionPayload.MaximumManifestBytes);
        File.WriteAllBytes(Path.Combine(_root, "extensions.json"), source);
        if (excess != 0)
        {
            await AssertPayloadRequestFailsAsync();
        }
        else
        {
            using (FileStream stream = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None))
            using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
            {
                byte[] filtered = ReadManifestTestEntry(archive, "extensions.json");
                Assert.Equal(1048576, filtered.Length);
                using JsonDocument document = JsonDocument.Parse(filtered);
                Assert.Equal(0, document.RootElement.GetProperty("extensions").GetArrayLength());
            }

            AssertNoTemporaryArchives();
        }

        Assert.Equal(source, File.ReadAllBytes(Path.Combine(_root, "extensions.json")));
    }

    private async Task AssertPayloadRequestFailsAsync()
    {
        using RecordingClient client = new();
        await CreateProducer().SendAsync("invalid-payload", client, CancellationToken.None);
        StreamingMessage terminal = Assert.Single(client.Messages);
        Assert.Equal("invalid-payload", terminal.RequestId);
        Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Result, terminal.ExtensionPayloadResponse.ContentCase);
        Assert.Equal(StatusResult.Types.Status.Failure, terminal.ExtensionPayloadResponse.Result.Status);
        Assert.Equal(0UL, terminal.ExtensionPayloadResponse.Offset);
        Assert.Null(terminal.ExtensionPayloadResponse.Result.Exception);
        Assert.DoesNotContain(_root, terminal.ExtensionPayloadResponse.Result.Result, StringComparison.Ordinal);
        AssertNoTemporaryArchives();
    }

    private static byte[] ReadManifestTestEntry(ZipArchive archive, string name)
    {
        ZipArchiveEntry entry = archive.GetEntry(name)!;
        Assert.NotNull(entry);
        using Stream input = entry.Open();
        using MemoryStream output = new();
        input.CopyTo(output);
        return output.ToArray();
    }
}
