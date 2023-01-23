using System.Text;
using Microsoft.Extensions.Logging;

namespace SampleIntegrationTests;

internal class NunitLogger<T> : NunitLogger, ILogger<T>
{
    /// <inheritdoc />
    public NunitLogger(LoggerExternalScopeProvider scopeProvider) 
        : base(scopeProvider, typeof(T).FullName)
    {
    }
}

internal class NunitLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LoggerExternalScopeProvider _scopeProvider;

    public NunitLogger(LoggerExternalScopeProvider scopeProvider, string categoryName)
    {
        _scopeProvider = scopeProvider;
        _categoryName = categoryName;
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) => _scopeProvider.Push(state);

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) =>  logLevel != LogLevel.None;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var sb = new StringBuilder();
        sb.Append(GetLogLevelString(logLevel))
            .Append(" [").Append(_categoryName).Append("] ")
            .Append(formatter(state, exception));

        if (exception != null)
        {
            sb.Append('\n').Append(exception);
        }

        // Append scopes
        _scopeProvider.ForEachScope((scope, state) =>
        {
            state.Append("\n => ");
            state.Append(scope);
        }, sb);

        TestContext.WriteLine(sb.ToString());
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace =>       "trce",
            LogLevel.Debug =>       "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning =>     "warn",
            LogLevel.Error =>       "fail",
            LogLevel.Critical =>    "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }
}
