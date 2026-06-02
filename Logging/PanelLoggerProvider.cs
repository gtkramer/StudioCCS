using System.Drawing;
using Microsoft.Extensions.Logging;
namespace StudioCCS.Logging
{
    /// <summary>
    /// A logging provider that routes log lines to the in-app log panel. Each line
    /// is handed to the sink as (tag, message, tagColour) so the panel can colour
    /// just the severity tag — matching the console — while the colour itself comes
    /// from the shared <see cref="LogPalette"/>. The sink is expected to marshal
    /// onto the UI thread (see MainWindow.AppendLog).
    /// </summary>
    public sealed class PanelLoggerProvider : ILoggerProvider
    {
        private readonly PanelLogger _logger;

        public PanelLoggerProvider(Action<string, string, Color> sink)
        {
            _logger = new PanelLogger(sink);
        }

        public ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose() { }

        private sealed class PanelLogger : ILogger
        {
            private readonly Action<string, string, Color> _sink;

            public PanelLogger(Action<string, string, Color> sink) => _sink = sink;

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

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

                _sink(LogLevelTag.Of(logLevel), text, ColorFor(logLevel));
            }

            private static Color ColorFor(LogLevel level)
            {
                var (r, g, b) = LogPalette.Of(level);
                return Color.FromArgb(255, r, g, b);
            }
        }
    }
}
