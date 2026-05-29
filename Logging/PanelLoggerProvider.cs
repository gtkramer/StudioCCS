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
				// Mirror the console provider's "<level>: <message>" shape (minus the
				// redundant category) so the panel reads like a copy of stdout. One
				// log call == one panel line, so no trailing newline here.
				_sink($"{ShortName(logLevel)}: {text}", ColorFor(logLevel));
			}

			// Matches the abbreviations the SimpleConsole provider prints on stdout.
			private static string ShortName(LogLevel level) => level switch
			{
				LogLevel.Trace => "trce",
				LogLevel.Debug => "dbug",
				LogLevel.Information => "info",
				LogLevel.Warning => "warn",
				LogLevel.Error => "fail",
				LogLevel.Critical => "crit",
				_ => "info",
			};

			// Bright variants chosen to stay readable on the panel's dark background.
			private static Color ColorFor(LogLevel level) => level switch
			{
				LogLevel.Critical => Color.FromArgb(255, 255, 85, 85),
				LogLevel.Error => Color.FromArgb(255, 255, 85, 85),
				LogLevel.Warning => Color.Orange,
				LogLevel.Information => Color.Gainsboro,
				_ => Color.Gray,
			};
		}
	}
}
