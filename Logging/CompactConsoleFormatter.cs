using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace StudioCCS.Logging
{
    /// <summary>
    /// A console formatter that prints "&lt;level&gt;: &lt;message&gt;" — dropping the
    /// category/eventId (always "StudioCCS[0]" here) that the built-in
    /// SimpleConsole formatter includes, so stdout matches the in-app log panel.
    /// The level tag is ANSI-coloured when writing to a real terminal.
    /// </summary>
    public sealed class CompactConsoleFormatter : ConsoleFormatter
    {
        public const string FormatterName = "compact";

        public CompactConsoleFormatter() : base(FormatterName) { }

        public override void Write<TState>(in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
            if (string.IsNullOrEmpty(message) && logEntry.Exception == null)
            {
                return;
            }

            // Colour only the level tag (message left at the terminal default), to
            // match the panel. Suppressed when stdout is redirected so piped/captured
            // logs stay free of escape codes.
            bool colour = !Console.IsOutputRedirected;
            if (colour)
            {
                textWriter.Write(LogPalette.Ansi(logEntry.LogLevel));
            }

            textWriter.Write(LogLevelTag.Of(logEntry.LogLevel));
            if (colour)
            {
                textWriter.Write("\x1b[0m");
            }

            textWriter.Write(": ");
            textWriter.WriteLine(message);

            if (logEntry.Exception != null)
            {
                textWriter.WriteLine(logEntry.Exception);
            }
        }
    }
}
