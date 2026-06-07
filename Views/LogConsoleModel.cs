using System.Collections.Concurrent;
using Avalonia.Collections;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace StudioCCS.Views;

/// <summary>
/// Backing store for the log panel, and the single place the panel's threading
/// lives. <see cref="Append"/> may be called from any thread — the background
/// parse fan-out, the GL render callback, or the UI thread — while everything it
/// touches downstream stays on the UI thread.
///
/// The hand-off is lock-free: lines are pushed onto a concurrent queue and a
/// single drain is posted to the UI thread, where each burst is applied to
/// <see cref="Lines"/> as one batched range update. Logging can emit thousands
/// of lines during a parse; collapsing each burst into one CollectionChanged
/// (rather than one per line) is what keeps the ListBox — and the UI — from
/// drowning. The buffer is capped so a heavy parse can't grow it without bound;
/// the oldest lines drop first.
/// </summary>
public sealed class LogConsoleModel
{
    // AvaloniaList (not ObservableCollection) so a whole burst is added/trimmed
    // with a single range notification the virtualizing ListBox absorbs in one
    // layout pass.
    private readonly AvaloniaList<LogLine> _lines = new AvaloniaList<LogLine>();
    public AvaloniaList<LogLine> Lines => _lines;

    // The hand-off point between the producing threads and the UI thread.
    private readonly ConcurrentQueue<(LogLevel Level, string Message)> _pending = new ConcurrentQueue<(LogLevel, string)>();
    private int _flushQueued;
    private readonly int _maxLines;

    public LogConsoleModel(int maxLines = 2000)
    {
        _maxLines = maxLines;
    }

    /// <summary>
    /// Records one log line. Safe to call from any thread. Interior newlines are
    /// split into separate lines so every row stays single-line: that keeps the
    /// list's row height uniform, which both the virtualizing panel's extent
    /// estimate and the auto-scroll math rely on.
    /// </summary>
    public void Append(LogLevel level, string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        // Fast path: the overwhelming majority of lines have no interior newline
        // (Log.Normalize already strips the trailing one), so avoid the Split
        // allocation for them.
        if (message.IndexOf('\n') < 0)
        {
            _pending.Enqueue((level, message));
        }
        else
        {
            foreach (string line in message.Split('\n'))
            {
                string trimmed = line.TrimEnd('\r');
                if (trimmed.Length > 0)
                {
                    _pending.Enqueue((level, trimmed));
                }
            }
        }

        // Ensure exactly one flush is in flight: the first enqueue posts the
        // drain, later ones ride that same pass. Posting at Background priority
        // lets a burst coalesce instead of scheduling a pass per line.
        if (Interlocked.Exchange(ref _flushQueued, 1) == 0)
        {
            Dispatcher.UIThread.Post(Flush, DispatcherPriority.Background);
        }
    }

    // Drains everything buffered so far into the list in one batch. Runs on the
    // UI thread.
    private void Flush()
    {
        // Clear the flag *before* draining: lines enqueued from here on schedule
        // a fresh flush, while everything already enqueued is drained by the loop
        // below. That ordering is what makes the lock-free hand-off lossless.
        Interlocked.Exchange(ref _flushQueued, 0);

        List<LogLine> batch = null;
        while (_pending.TryDequeue(out var pending))
        {
            (batch ??= new List<LogLine>()).Add(new LogLine(pending.Level, pending.Message));
        }

        if (batch == null)
        {
            return;
        }

        _lines.AddRange(batch);

        // Cap the buffer; drop the oldest lines first, again as one range removal.
        int over = _lines.Count - _maxLines;
        if (over > 0)
        {
            _lines.RemoveRange(0, over);
        }
    }
}
