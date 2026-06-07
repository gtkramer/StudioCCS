using Microsoft.Extensions.Logging;

namespace StudioCCS.Logging;

/// <summary>
/// A logging provider that routes log lines to the in-app log panel. Each line
/// is handed to the sink as (level, message); the panel derives the tag and its
/// colour from the level itself (the tag via <see cref="LogLevelTag"/>, the
/// colour from the theme so it tracks light/dark). The sink is expected to
/// marshal onto the UI thread — see <c>LogConsoleModel.Append</c>, which is
/// safe to call from any thread.
/// </summary>
public sealed class PanelLoggerProvider : ILoggerProvider
{
    private readonly PanelLogger _logger;

    public PanelLoggerProvider(Action<LogLevel, string> sink)
    {
        _logger = new PanelLogger(sink);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose() { }

    private sealed class PanelLogger : ILogger
    {
        private readonly Action<LogLevel, string> _sink;

        public PanelLogger(Action<LogLevel, string> sink)
        {
            _sink = sink;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string text = formatter(state, exception);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _sink(logLevel, text);
        }
    }
}
