using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace StudioCCS.Views;

/// <summary>
/// Lets a TreeViewItem expand/collapse when its whole row (the label) is tapped,
/// not just the small chevron. Wire it with
/// <c>tree.Tapped += TreeViewExpand.ToggleOnTap</c>.
/// </summary>
internal static class TreeViewExpand
{
    public static void ToggleOnTap(object sender, TappedEventArgs e)
    {
        if (!(e.Source is Visual source))
        {
            return;
        }

        // The expand/collapse chevron is a ToggleButton that already toggles
        // itself; ignore taps on it so we don't toggle twice (a no-op).
        if (source.FindAncestorOfType<ToggleButton>(includeSelf: true) != null)
        {
            return;
        }

        var item = source.FindAncestorOfType<TreeViewItem>(includeSelf: true);
        if (item != null && item.ItemCount > 0)
        {
            item.IsExpanded = !item.IsExpanded;
        }
    }
}
