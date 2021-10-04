// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.E2ETests.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    public class FunctionAppFixture : IAsyncLifetime
    {
        private readonly ILogger _logger;
        private bool _disposed;
        private Process _funcProcess;

        private JobObjectRegistry _jobObjectRegistry;

        public FunctionAppFixture(IMessageSink messageSink)
        {
            // initialize logging            
            ILoggerFactory loggerFactory = new LoggerFactory();
            TestLogs = new TestLoggerProvider(messageSink);
            loggerFactory.AddProvider(TestLogs);
            _logger = loggerFactory.CreateLogger<FunctionAppFixture>();
        }

        public async Task InitializeAsync()
        {
            // start host via CLI if testing locally
            if (Constants.FunctionsHostUrl.Contains("localhost"))
            {
                // kill existing func processes
                _logger.LogInformation("Shutting down any running functions hosts..");
                FixtureHelpers.KillExistingFuncHosts();

                // start functions process
                _logger.LogInformation($"Starting functions host for {Constants.FunctionAppCollectionName}...");
                _funcProcess = FixtureHelpers.GetFuncHostProcess();
                string workingDir = _funcProcess.StartInfo.WorkingDirectory;
                _logger.LogInformation($"  Working dir: '${workingDir}' Exists: '{Directory.Exists(workingDir)}'");
                string fileName = _funcProcess.StartInfo.FileName;
                _logger.LogInformation($"  File name:   '${fileName}' Exists: '{File.Exists(fileName)}'");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Currently only HTTP is supported in Linux CI.
                    _funcProcess.StartInfo.ArgumentList.Add("--functions");
                    _funcProcess.StartInfo.ArgumentList.Add("HelloFromQuery");
                    _funcProcess.StartInfo.ArgumentList.Add("HelloFromJsonBody");
                    _funcProcess.StartInfo.ArgumentList.Add("HelloUsingPoco");
                    _funcProcess.StartInfo.ArgumentList.Add("ExceptionFunction");
                }

                await CosmosDBHelpers.TryCreateDocumentCollectionsAsync(_logger);

                FixtureHelpers.StartProcessWithLogging(_funcProcess, _logger);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // ensure child processes are cleaned up
                    _jobObjectRegistry = new JobObjectRegistry();
                    _jobObjectRegistry.Register(_funcProcess);
                }

                var httpClient = new HttpClient();
                _logger.LogInformation("Waiting for host to be running...");
                await TestUtility.RetryAsync(async () =>
                {
                    try
                    {
                        var response = await httpClient.GetAsync($"{Constants.FunctionsHostUrl}/admin/host/status");
                        var content = await response.Content.ReadAsStringAsync();
                        var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.TryGetProperty("state", out JsonElement value) &&
                            value.GetString() == "Running")
                        {
                            _logger.LogInformation($"  Current state: Running");
                            return true;
                        }

                        _logger.LogInformation($"  Current state: {value}");
                        return false;
                    }
                    catch
                    {
                        // Can get exceptions before host is running.
                        _logger.LogInformation($"  Current state: process starting");
                        return false;
                    }
                }, userMessageCallback: () => string.Join(System.Environment.NewLine, TestLogs.CoreToolsLogs));
            }
        }

        internal TestLoggerProvider TestLogs { get; private set; }


        public Task DisposeAsync()
        {
            if (!_disposed)
            {
                _logger.LogInformation("FunctionAppFixture disposing.");

                if (_funcProcess != null)
                {
                    try
                    {
                        _logger.LogInformation($"Shutting down functions host for {Constants.FunctionAppCollectionName}");
                        _funcProcess.Kill();
                        _funcProcess.Dispose();
                    }
                    catch
                    {
                        // process may not have started
                    }
                }

                _jobObjectRegistry?.Dispose();
            }

            _disposed = true;

            return Task.CompletedTask;
        }
    }

    [CollectionDefinition(Constants.FunctionAppCollectionName)]
    public class FunctionAppCollection : ICollectionFixture<FunctionAppFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
