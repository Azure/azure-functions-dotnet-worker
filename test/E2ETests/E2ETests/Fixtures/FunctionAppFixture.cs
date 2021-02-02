// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.E2ETests.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    public class FunctionAppFixture : IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed;
        private Process _funcProcess;

        public FunctionAppFixture(IMessageSink messageSink)
        {
            // initialize logging            
            ILoggerFactory loggerFactory = new LoggerFactory();
            TestLogs = new TestLoggerProvider(messageSink);
            loggerFactory.AddProvider(TestLogs);
            _logger = loggerFactory.CreateLogger<FunctionAppFixture>();

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

                FixtureHelpers.StartProcessWithLogging(_funcProcess, _logger);

                TestUtility.RetryAsync(() =>
                {
                    return Task.FromResult(TestLogs.CoreToolsLogs.Contains("Host lock lease acquired by instance ID"));
                }).GetAwaiter().GetResult();
            }

            CosmosDBHelpers.CreateDocumentCollections().GetAwaiter().GetResult();
        }

        internal TestLoggerProvider TestLogs { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logger.LogInformation("FunctionAppFixture disposing.");

                    if (_funcProcess != null)
                    {
                        _logger.LogInformation($"Shutting down functions host for {Constants.FunctionAppCollectionName}");
                        _funcProcess.Kill();
                        _funcProcess.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
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
