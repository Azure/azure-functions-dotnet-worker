// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace  Microsoft.Azure.Functions.Worker.Logging
{
    /// <summary>
    /// Service responsible for logging the binding type that has been resolved for each function.
    /// This log only occurs once 10 seconds the worker has been initialized to avoid cold start
    /// impact.
    /// </summary>
    public class BindingLoggerHostedService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly Timer _timer;
        private readonly int _due = 10000; // 10 seconds
        private bool _disposed;

        /// <summary>
        /// TODO: should we be using IWorkerDiagnostics?
        /// </summary>
        public BindingLoggerHostedService(ILogger<BindingLoggerHostedService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timer = new Timer(PublishLogs);
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_disposed || _timer is null)
            {
                return Task.FromException(new InvalidOperationException("Service has already been disposed"));
            }

            try
            {
                // start the timer by setting the due time
                _timer.Change(_due, Timeout.Infinite);
            }
            catch (Exception exc)
            {
                _logger.LogWarning(exc, "Unable to set timer interval");
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // stop the timer if it has been started
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Log the binding type that has been resolved for each function
        /// </summary>
        private void PublishLogs(object? state)
        {
            try
            {
                // TODO: figure out how to get the resolved binding type
                _logger.LogInformation("Function 'blah' resolved binding to 'blah'");
                // or
                // _diagnostics.BindingResolved();
            }
            catch (Exception exc)
            {
                _logger.LogWarning(exc, "Failed to publish binding logs");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _timer?.Dispose();
            _disposed = true;
        }
    }
}