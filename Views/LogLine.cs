using Microsoft.Extensions.Logging;
using StudioCCS.Logging;

namespace StudioCCS.Views;

/// <summary>
/// One rendered line in the log panel: the severity tag (e.g. "warn") and the
/// message. The line carries its <em>severity</em>, not a pre-built brush — the
/// view resolves the tag colour from the theme-aware Log* resources via the
/// <c>warn</c>/<c>error</c> classes below, so the panel tracks the OS light/dark
/// setting and a colour never has to be marshalled in with the text. Only the
/// tag is coloured; the message is the neutral LogForeground, matching the
/// console.
/// </summary>
public sealed class LogLine
{
    public string Tag { get; }
    public string Message { get; }

    /// <summary>Drives the tag's <c>warn</c> style class (orange/amber).</summary>
    public bool IsWarn { get; }

    /// <summary>Drives the tag's <c>error</c> style class (red); also covers Critical.</summary>
    public bool IsError { get; }

    public LogLine(LogLevel level, string message)
    {
        Tag = LogLevelTag.Of(level);
        Message = message;
        IsWarn = level == LogLevel.Warning;
        IsError = level is LogLevel.Error or LogLevel.Critical;
    }
}
