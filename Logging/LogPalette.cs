using Microsoft.Extensions.Logging;

namespace StudioCCS
{
	/// <summary>
	/// Single source of truth for severity colours, shared by the console
	/// formatter and the panel provider so stdout and the in-app log view are
	/// always coloured identically. Defined as RGB: the console emits a 24-bit
	/// (truecolor) ANSI escape, the panel builds a brush from the same values.
	/// Tuned to read on a dark background (the panel fixes one; most terminals
	/// use one too).
	/// </summary>
	internal static class LogPalette
	{
		public static (byte R, byte G, byte B) Of(LogLevel level) => level switch
		{
			LogLevel.Critical => (255, 85, 85),    // red
			LogLevel.Error => (255, 85, 85),        // red
			LogLevel.Warning => (255, 165, 0),      // orange
			LogLevel.Information => (220, 220, 220), // light grey
			_ => (128, 128, 128),                   // grey (trace/debug)
		};

		/// <summary>24-bit (truecolor) ANSI foreground escape for the level.</summary>
		public static string Ansi(LogLevel level)
		{
			var (r, g, b) = Of(level);
			return $"\x1b[38;2;{r};{g};{b}m";
		}
	}
}
