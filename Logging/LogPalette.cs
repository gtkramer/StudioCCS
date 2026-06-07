using Microsoft.Extensions.Logging;

namespace StudioCCS.Logging;

/// <summary>
/// Severity colours for the console log output. The console emits a 24-bit
/// (truecolor) ANSI escape built from these RGB values. The in-app log panel
/// does NOT use these — it has its own theme-aware palette (the Log* brushes in
/// App.axaml) so its colours flip with the light/dark setting, whereas a
/// terminal is effectively always dark. Tuned to read on that dark background.
/// </summary>
internal static class LogPalette
{
    public static (byte R, byte G, byte B) Of(LogLevel level)
    {
        return level switch
        {
            LogLevel.Critical => (255, 85, 85),    // red
            LogLevel.Error => (255, 85, 85),        // red
            LogLevel.Warning => (255, 165, 0),      // orange
            LogLevel.Information => (220, 220, 220), // light grey
            _ => (128, 128, 128),                   // grey (trace/debug)
        };
    }

    /// <summary>24-bit (truecolor) ANSI foreground escape for the level.</summary>
    public static string Ansi(LogLevel level)
    {
        var (r, g, b) = Of(level);
        return $"\x1b[38;2;{r};{g};{b}m";
    }
}
