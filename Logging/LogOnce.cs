using System.Collections.Concurrent;

namespace StudioCCS
{
	/// <summary>
	/// Flood protection for messages emitted from per-frame / per-bone code paths
	/// (e.g. an invalid NodeID in a render loop). Keys on the message text itself,
	/// so distinct messages can never collide the way the old GetHashCode() keys
	/// could. Thread-safe: logging can originate on the background parse thread.
	/// </summary>
	internal static class LogOnce
	{
		private static readonly ConcurrentDictionary<string, byte> Seen = new();

		/// <summary>Returns true the first time a given message is seen, false thereafter.</summary>
		public static bool FirstTime(string message) => Seen.TryAdd(message ?? string.Empty, 0);
	}
}
