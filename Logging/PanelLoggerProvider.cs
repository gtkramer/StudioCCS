using System;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace StudioCCS
{
	/// <summary>
	/// A logging provider that routes formatted messages to the in-app log panel.
	/// Severity-to-colour is decided here, in the view layer, instead of being
	/// baked into the log core: this is the only place System.Drawing.Color is
	/// still involved in logging. The supplied sink is expected to marshal onto
	/// the UI thread (see MainWindow.AppendLog).
	/// </summary>
	public sealed class PanelLoggerProvider : ILoggerProvider
	{
		private readonly PanelLogger _logger;

		public PanelLoggerProvider(Action<string, Color> sink)
		{
			_logger = new PanelLogger(sink);
		}

		public ILogger CreateLogger(string categoryName) => _logger;

		public void Dispose() { }

		private sealed class PanelLogger : ILogger
		{
			private readonly Action<string, Color> _sink;

			public PanelLogger(Action<string, Color> sink) => _sink = sink;

			public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

			public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
				Func<TState, Exception, string> formatter)
			{
				if (!IsEnabled(logLevel)) return;
				string text = formatter(state, exception);
				if (string.IsNullOrEmpty(text)) return;
				_sink(text + "\n", ColorFor(logLevel));
			}

			private static Color ColorFor(LogLevel level) => level switch
			{
				LogLevel.Critical => Color.DarkRed,
				LogLevel.Error => Color.DarkRed,
				LogLevel.Warning => Color.Orange,
				LogLevel.Information => Color.White,
				_ => Color.Gray,
			};
		}
	}
}
