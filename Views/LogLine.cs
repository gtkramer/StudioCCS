using Avalonia.Media;

namespace StudioCCS.Views
{
	/// <summary>
	/// One rendered line in the log panel: the formatted text (already carrying
	/// its "&lt;level&gt;: " prefix) plus the brush for its severity colour.
	/// </summary>
	public sealed class LogLine
	{
		public string Text { get; }
		public IBrush Brush { get; }

		public LogLine(string text, IBrush brush)
		{
			Text = text;
			Brush = brush;
		}
	}
}
