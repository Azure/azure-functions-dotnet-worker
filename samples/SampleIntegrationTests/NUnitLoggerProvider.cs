using Microsoft.Extensions.Logging;

namespace SampleIntegrationTests;

internal sealed class NUnitLoggerProvider : ILoggerProvider
{
    private readonly LoggerExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName)
    {
        return new NunitLogger(_scopeProvider, categoryName);
    }

    public void Dispose()
    {
    }
}
