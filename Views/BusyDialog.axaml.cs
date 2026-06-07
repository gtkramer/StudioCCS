using Avalonia.Controls;

namespace StudioCCS.Views;

/// <summary>
/// A small modal "busy" dialog with an indeterminate progress bar, shown while a
/// long operation runs off the UI thread. The user cannot close it — only the
/// owner can, by calling <see cref="AllowClose"/> then <see cref="Window.Close()"/>
/// — so the owner stays blocked (and the scene stays un-mutated) for the whole
/// operation.
/// </summary>
public partial class BusyDialog : Window
{
    private bool _allowClose;

    public BusyDialog()
    {
        InitializeComponent();
    }

    public void SetMessage(string message)
    {
        messageText.Text = message;
    }

    /// <summary>Lets the next <see cref="Window.Close()"/> actually close the dialog.</summary>
    public void AllowClose()
    {
        _allowClose = true;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Ignore user-initiated closes (window manager, Alt+F4): closing early
        // would re-enable the owner and let the scene be mutated mid-operation.
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }
}
