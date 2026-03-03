// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Azure.Functions.Sdk.ZipDeploy;
using Microsoft.Build.Framework;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Azure.Functions.Sdk.Tasks.Publish;

public sealed class ZipDeployTask(IFileSystem fileSystem, DeploymentClient? client = null)
    : Microsoft.Build.Utilities.Task, ICancelableTask, IDisposable
{
    internal static readonly ProductInfoHeaderValue SdkUserAgentHeader = new(
        ThisAssembly.Name, ThisAssembly.Version.ToString(fieldCount: 3));

    internal static readonly ProductInfoHeaderValue OsUserAgentHeader = new(
        $"({RuntimeInformation.OSDescription}; {RuntimeInformation.OSArchitecture})");

    private static readonly TimeSpan DeployTimeout = TimeSpan.FromMinutes(3);

    private readonly CancellationTokenSource _cts = new();
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);
    private DeploymentClient? _deploymentClient = client;

    public ZipDeployTask()
        : this(new FileSystem())
    {
    }

    [Required]
    public string ZipContentsPath { get; set; } = string.Empty;

    [Required]
    public string DeploymentUsername { get; set; } = string.Empty;

    [Required]
    public string DeploymentPassword { get; set; } = string.Empty;

    [Required]
    public string PublishUrl { get; set; } = string.Empty;

    public string DotnetSdkVersion { get; set; } = "<unknown>";

    public bool UseBlobContainerDeploy { get; set; }

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
        Log.TaskResources = Strings.ResourceManager;
        if (!_fileSystem.File.Exists(ZipContentsPath))
        {
            Log.LogErrorFromResources(nameof(Strings.Deploy_ZipNotFound), ZipContentsPath);
            return false;
        }

        if (!InitClient())
        {
            return false;
        }

        using CancellationTokenSource timeout = new(DeployTimeout);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeout.Token);
        return DeployAsync(_deploymentClient, linked.Token).GetAwaiter().GetResult();
    }

    internal HttpClient BuildHttpClient(Uri publishUri) // internal for testing.
    {
        ProductInfoHeaderValue dotnetSdkHeader = new("Microsoft.NET.Sdk", DotnetSdkVersion);
        return new()
        {
            BaseAddress = publishUri,
            DefaultRequestHeaders =
            {
                UserAgent = { SdkUserAgentHeader, dotnetSdkHeader, OsUserAgentHeader }
            },
        };
    }

    private async Task<bool> DeployAsync(DeploymentClient client, CancellationToken cancellation)
    {
        Log.LogMessageFromResources(
            MessageImportance.High, nameof(Strings.Deploy_BeginPublish), ZipContentsPath, PublishUrl);

        using FileSystemStream content = _fileSystem.File.OpenRead(ZipContentsPath);
        ZipDeployRequest request = new(DeploymentUsername, DeploymentPassword, content)
        {
            UseBlobContainer = UseBlobContainerDeploy,
        };

        Uri uri = request.GetUri(new Uri(PublishUrl));
        try
        {
            DeployStatus state = await client.ZipDeployAsync(request, cancellation);
            if (state is DeployStatus.Success or DeployStatus.PartialSuccess)
            {
                Log.LogMessageFromResources(nameof(Strings.Deploy_CompletedSuccess), ZipContentsPath, uri, state);
                return true;
            }
            else
            {
                Log.LogErrorFromResources(nameof(Strings.Deploy_CompletedFailure), ZipContentsPath, uri, state);
                return false;
            }
        }
        catch (DeploymentException ex)
        {
            Log.LogErrorFromResources(
                nameof(Strings.Deploy_Failed), ex.Message, uri, ex.StatusCode, ex.DeployStatus);
            return false;
        }
    }

    [MemberNotNullWhen(true, nameof(_deploymentClient))]
    private bool InitClient()
    {
        if (_deploymentClient is not null)
        {
            return true;
        }

        if (!TryParsePublishUri(out Uri? publishUri))
        {
            Log.LogErrorFromResources(nameof(Strings.Deploy_InvalidPublishUrl), PublishUrl);
            return false;
        }

        HttpClient client = BuildHttpClient(publishUri);
        _deploymentClient = new(client, Log);
        return true;
    }

    private bool TryParsePublishUri([NotNullWhen(true)] out Uri? uri)
    {
        static bool IsHttp(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        if (!Uri.TryCreate(PublishUrl, UriKind.Absolute, out uri) || !IsHttp(uri))
        {
            Log.LogError(nameof(Strings.Deploy_InvalidPublishUrl), PublishUrl);
            return false;
        }

        return true;
    }
}

