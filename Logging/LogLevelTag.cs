using Microsoft.Extensions.Logging;

namespace StudioCCS
{
	/// <summary>
	/// The short severity tag used as a line's "&lt;level&gt;: " prefix. Shared by
	/// the console formatter and the panel provider so stdout and the in-app log
	/// view render identically. Matches the abbreviations the built-in
	/// SimpleConsole formatter prints.
	/// </summary>
	internal static class LogLevelTag
	{
		public static string Of(LogLevel level) => level switch
		{
			LogLevel.Trace => "trce",
			LogLevel.Debug => "dbug",
			LogLevel.Information => "info",
			LogLevel.Warning => "warn",
			LogLevel.Error => "fail",
			LogLevel.Critical => "crit",
			_ => "info",
		};
	}
}
