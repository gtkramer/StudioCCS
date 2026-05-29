using Avalonia.Media;

namespace StudioCCS.Views
{
	/// <summary>
	/// One rendered line in the log panel: the severity tag (e.g. "warn") and its
	/// colour, plus the message. Only the tag is coloured; the message is rendered
	/// in a neutral colour by the template, matching the console.
	/// </summary>
	public sealed class LogLine
	{
		public string Tag { get; }
		public string Message { get; }
		public IBrush TagBrush { get; }

		public LogLine(string tag, string message, IBrush tagBrush)
		{
			Tag = tag;
			Message = message;
			TagBrush = tagBrush;
		}
	}
}
