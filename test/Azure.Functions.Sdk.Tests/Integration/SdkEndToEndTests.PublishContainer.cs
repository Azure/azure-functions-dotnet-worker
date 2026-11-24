// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NET
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Xunit.Sdk;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Net11ContainerTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task PublishContainer_Net11_StartsHostAndExecutesFunction(bool useAspNetIntegration, bool selfContained)
    {
        string repository = $"functions-sdk-net11-{Guid.NewGuid():N}";
        string image = $"{repository}:latest";
        string container = $"functions-sdk-net11-{Guid.NewGuid():N}";
        string publishPath = Path.Combine(TestRootPath, "publish");
        WriteNet11ContainerProject(useAspNetIntegration);
        bool imageCreated = false;
        bool containerCreated = false;

        try
        {
            if (useAspNetIntegration)
            {
                await RunProcess("dotnet", "build", "FunctionApp.csproj", "-c", "Release");
            }

            List<string> arguments =
            [
                "publish", "FunctionApp.csproj", "-c", "Release", "-t:PublishContainer",
                "-o", publishPath, $"-p:ContainerRepository={repository}", "-p:ContainerImageTag=latest", "-p:LocalRegistry=Docker",
            ];
            if (useAspNetIntegration)
            {
                arguments.AddRange([
                    "--no-build",
                    "--no-restore",
                    "-p:ContainerBaseImage=mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated11.0-chiseled",
                ]);
            }
            if (selfContained)
            {
                arguments.AddRange(["-r", "linux-x64", "-p:PublishSelfContained=true"]);
            }
            await RunProcess("dotnet", arguments.ToArray());
            imageCreated = true;

            using (JsonDocument config = JsonDocument.Parse(File.ReadAllText(Path.Combine(publishPath, "worker.config.json"))))
            {
                config.RootElement.GetProperty("description").GetProperty("defaultExecutablePath").GetString()
                    .Should().Be(selfContained ? "{WorkerRoot}FunctionApp" : "dotnet");
            }
            Directory.Exists(Path.Combine(publishPath, "usr")).Should().BeFalse();
            if (useAspNetIntegration)
            {
                await RunProcess("dotnet", "msbuild", "FunctionApp.csproj",
                    "-t:_PrepareFunctionsChiseledContainer;_CleanupFunctionsChiseledContainer",
                    "-p:_FunctionsUseChiseledContainer=true",
                    "-p:ContainerWorkingDirectory=/home/site/wwwroot",
                    $"-p:PublishDir={publishPath}{Path.DirectorySeparatorChar}",
                    $"-p:MSBuildProjectExtensionsPath={Path.Combine(TestRootPath, "obj")}{Path.DirectorySeparatorChar}",
                    $"-p:ProjectAssetsFile={Path.Combine(TestRootPath, "obj", "project.assets.json")}");
                using JsonDocument assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRootPath, "obj", "project.assets.json")));
                assets.RootElement.GetProperty("project").GetProperty("restore").GetProperty("projectName").GetString()
                    .Should().Be("FunctionApp");

                await VerifyFailedRuntimeRestoreCleansStaging(publishPath);
            }

            await RunProcess("docker", "create", "--name", container,
                "-e", "AzureWebJobsSecretStorageType=files", "-p", "127.0.0.1::80", image);
            containerCreated = true;
            await RunProcess("docker", "start", container);
            string portOutput = await RunProcess("docker", "port", container, "80/tcp");
            int port = int.Parse(portOutput.Trim().Split(':').Last());
            string body = await WaitForFunction(container, port);

            using JsonDocument result = JsonDocument.Parse(body);
            result.RootElement.GetProperty("runtime").GetString().Should().StartWith(".NET 11.");
            if (useAspNetIntegration)
            {
                result.RootElement.GetProperty("aspNetRuntime").GetString().Should().StartWith("11.");
            }
            if (!selfContained)
            {
                result.RootElement.GetProperty("runtimeDirectory").GetString()
                    .Should().StartWith("/usr/share/dotnet/shared/Microsoft.NETCore.App/11.");
            }
        }
        finally
        {
            if (containerCreated)
            {
                await RunProcess("docker", "rm", "-f", container);
            }
            if (imageCreated)
            {
                await RunProcess("docker", "image", "rm", image);
            }
        }
    }

    private async Task VerifyFailedRuntimeRestoreCleansStaging(string publishPath)
    {
        string runtimeConfigPath = Path.Combine(publishPath, "FunctionApp.runtimeconfig.json");
        string originalConfig = File.ReadAllText(runtimeConfigPath);
        string emptySource = Path.Combine(TestRootPath, "empty-source");
        Directory.CreateDirectory(emptySource);
        try
        {
            File.WriteAllText(runtimeConfigPath, originalConfig.Replace("11.0.0", "11.0.7654321"));
            XunitException error = await Assert.ThrowsAsync<XunitException>(() => RunProcess("dotnet", "msbuild", "FunctionApp.csproj",
                "-t:_PrepareFunctionsChiseledContainer", "-p:Configuration=Release",
                "-p:_FunctionsUseChiseledContainer=true",
                "-p:ContainerWorkingDirectory=/home/site/wwwroot",
                $"-p:PublishDir={publishPath}{Path.DirectorySeparatorChar}",
                $"-p:RestoreSources={emptySource}",
                $"-p:RestorePackagesPath={Path.Combine(TestRootPath, "empty-packages")}"));
            error.Message.Should().Contain("error NU");
            string stagingParent = Path.Combine(TestRootPath, "obj", "Release", "net11.0", "azure_functions_container");
            Directory.GetDirectories(stagingParent).Should().BeEmpty();
        }
        finally
        {
            File.WriteAllText(runtimeConfigPath, originalConfig);
        }
    }

    private void WriteNet11ContainerProject(bool useAspNetIntegration)
    {
        File.WriteAllText(Path.Combine(TestRootPath, "global.json"), """
            { "sdk": { "version": "11.0.100", "rollForward": "latestFeature" } }
            """);
        string extension = useAspNetIntegration
            ? "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore"
            : "Microsoft.Azure.Functions.Worker.Extensions.Http";
        string extensionVersion = useAspNetIntegration ? "2.1.1" : "3.3.0";
        File.WriteAllText(Path.Combine(TestRootPath, "FunctionApp.csproj"), $$"""
            <Project Sdk="Azure.Functions.Sdk/99.99.99">
              <PropertyGroup>
                <TargetFramework>net11.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.52.0" />
                <PackageReference Include="{{extension}}" Version="{{extensionVersion}}" />
                <None Update="host.json" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(TestRootPath, "host.json"), """{ "version": "2.0" }""");
        string source = useAspNetIntegration ? """
            using System.Runtime.InteropServices;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Azure.Functions.Worker.Builder;

            var builder = FunctionsApplication.CreateBuilder(args);
            builder.ConfigureFunctionsWebApplication();
            await builder.Build().RunAsync();

            public class Probe
            {
                [Function("Probe")]
                public IActionResult Run(
                    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "probe")] HttpRequest request)
                {
                    return new OkObjectResult(new
                    {
                        runtime = RuntimeInformation.FrameworkDescription,
                        aspNetRuntime = typeof(HttpRequest).Assembly.GetName().Version!.ToString(),
                        runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory()
                    });
                }
            }
            """ : """
            using System.Net;
            using System.Runtime.InteropServices;
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Azure.Functions.Worker.Http;
            using Microsoft.Extensions.Hosting;

            new HostBuilder().ConfigureFunctionsWorkerDefaults().Build().Run();

            public class Probe
            {
                [Function("Probe")]
                public async Task<HttpResponseData> Run(
                    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "probe")] HttpRequestData request)
                {
                    HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(new
                    {
                        runtime = RuntimeInformation.FrameworkDescription,
                        runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory()
                    });
                    return response;
                }
            }
            """;
        File.WriteAllText(Path.Combine(TestRootPath, "Program.cs"), source);
    }

    private async Task<string> WaitForFunction(string container, int port)
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(2) };
        DateTime deadline = DateTime.UtcNow.AddSeconds(90);
        string lastFailure = string.Empty;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync($"http://127.0.0.1:{port}/api/probe");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                lastFailure = $"HTTP {(int)response.StatusCode}";
            }
            catch (HttpRequestException exception)
            {
                lastFailure = exception.Message;
            }
            catch (TaskCanceledException exception)
            {
                lastFailure = exception.Message;
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        string logs = await RunProcess("docker", "logs", "--tail", "100", container);
        throw new XunitException($"The function did not respond: {lastFailure}{Environment.NewLine}{logs}");
    }

    private async Task<string> RunProcess(string executable, params string[] arguments)
    {
        ProcessStartInfo startInfo = new(executable)
        {
            WorkingDirectory = TestRootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["MSBUILDUSESERVER"] = "0";
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        startInfo.Environment.Remove("MSBuildSDKsPath");
        startInfo.Environment.Remove("MSBUILD_EXE_PATH");
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(3));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{executable} did not finish within three minutes.");
        }

        string output = await stdout;
        string error = await stderr;
        if (process.ExitCode != 0)
        {
            throw new XunitException($"{executable} exited with code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
        }

        return output + error;
    }
}

internal sealed class Net11ContainerTheoryAttribute : TheoryAttribute
{
    public Net11ContainerTheoryAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_FUNCTIONS_CONTAINER_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Requires .NET 11 SDK and a running Docker daemon; set RUN_FUNCTIONS_CONTAINER_TESTS=true.";
        }
    }
}
#endif
