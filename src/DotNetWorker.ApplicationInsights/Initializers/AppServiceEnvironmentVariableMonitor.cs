using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers;

internal class AppServiceEnvironmentVariableMonitor : BackgroundService, IOptionsChangeTokenSource<AppServiceOptions>
{
    private readonly TimeSpan _refreshInterval;

    private IChangeToken _changeToken;
    private CancellationTokenSource _cancellationTokenSource = new();

    private readonly Dictionary<string, string?> _monitoredVariableCache = new(StringComparer.OrdinalIgnoreCase);

    public AppServiceEnvironmentVariableMonitor() : this(TimeSpan.FromSeconds(5))
    {
    }

    public AppServiceEnvironmentVariableMonitor(TimeSpan refreshInterval)
    {
        _refreshInterval = refreshInterval;
        _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);
    }

    public string Name => string.Empty;

    public IChangeToken GetChangeToken() => _changeToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool changeDetected = false;

            foreach (string envVar in AppServiceOptionsInitializer.EnvironmentVariablesToMonitor)
            {
                string? currentVal = Environment.GetEnvironmentVariable(envVar);
                _monitoredVariableCache.TryGetValue(envVar, out string? cachedVal);

                if (!string.Equals(currentVal, cachedVal, StringComparison.Ordinal))
                {
                    changeDetected = true;
                    _monitoredVariableCache[envVar] = currentVal;
                }
            }

            if (changeDetected)
            {
                var oldTokenSource = Interlocked.Exchange(ref _cancellationTokenSource, new CancellationTokenSource());
                Interlocked.Exchange(ref _changeToken, new CancellationChangeToken(_cancellationTokenSource.Token));

                if (!oldTokenSource.IsCancellationRequested)
                {
                    oldTokenSource.Cancel();
                    oldTokenSource.Dispose();
                }
            }

            try
            {
                await Task.Delay(_refreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // happens during normal shutdown
                break;
            }
        }
    }
}
