using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OpenTK.Mathematics;
using StudioCCS.FileFormat;
using StudioCCS.FileFormat.Animation;
using StudioCCS.FileFormat.Geometry;
using StudioCCS.Logging;
using StudioCCS.Rendering;
using StudioCCS.ViewModels;

namespace StudioCCS.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new MainViewModel();
    private readonly DispatcherTimer _statusTimer;

    // The log panel's backing store. Capped so a heavy parse can't grow it
    // without bound; oldest lines are dropped first. Touched only on the UI
    // thread (AppendLog marshals there first).
    private readonly ObservableCollection<LogLine> _logLines = new();
    private readonly Dictionary<uint, IBrush> _brushCache = new();
    private const int MaxLogLines = 2000;

    // Incoming lines are buffered here and flushed to _logLines in batches.
    // Logging can burst thousands of lines during a parse; touching the ListBox
    // once per line (a dispatcher post, a CollectionChanged, and a layout-forcing
    // ScrollIntoView each) floods the UI thread and is what kills performance.
    // Batching collapses each burst into a single UI update. The queue is the
    // hand-off point between the producing threads and the UI thread.
    private readonly ConcurrentQueue<(string Tag, string Message, System.Drawing.Color Color)> _pendingLogs = new();
    private int _logFlushQueued;

    public MainWindow() : this(null)
    {
    }

    public MainWindow(string[] startupFiles)
    {
        InitializeComponent();
        DataContext = _vm;

        logView.ItemsSource = _logLines;

        // Configure logging: the framework owns levels/filtering and the stdout
        // sink; our custom provider routes to the log panel (marshalled onto the
        // UI thread by AppendLog).
        Log.Init(b => b
            .SetMinimumLevel(LogLevel.Information)
            .AddConsole(o => o.FormatterName = CompactConsoleFormatter.FormatterName)
            .AddConsoleFormatter<CompactConsoleFormatter, ConsoleFormatterOptions>()
            .AddProvider(new PanelLoggerProvider(AppendLog)));

        // Let a whole tree row toggle expand/collapse, not just the chevron.
        ccsTree.Tapped += TreeViewExpand.ToggleOnTap;
        sceneTree.Tapped += TreeViewExpand.ToggleOnTap;

        // The GL viewport's clear/grid colours aren't styled by the theme engine
        // (they're set in raw GL), so push theme-appropriate values into Scene
        // now and again whenever the OS light/dark setting changes. Scene re-reads
        // these every frame, so the switch is reflected immediately.
        ActualThemeVariantChanged += (_, _) => ApplyViewportTheme();
        ApplyViewportTheme();

        // Drag & drop CCS files onto the window.
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        // SMD export was a debug-only feature in the original (gated behind #if DEBUG).
#if DEBUG
        miDumpSmd.IsVisible = true;
        miDumpPreviewSmd.IsVisible = true;
#endif

        // The toolbar RadioButtons set _vm.Mode; mirror mode changes into the
        // panel layout (and apply the initial layout, since the VM's default
        // Mode is set before this handler is attached).
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Mode))
            {
                ApplyModeLayout(_vm.Mode);
            }
        };
        ApplyModeLayout(_vm.Mode);

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _statusTimer.Tick += (_, _) => _vm.RefreshCameraStatus();
        _statusTimer.Start();

        // Optional files passed on the command line (e.g. "open with").
        if (startupFiles != null)
        {
            LoadFiles(startupFiles.Where(File.Exists));
        }
    }

    // Maps the active theme variant onto the viewport's GL clear and grid colours.
    // Dark keeps the original neutral-grey look; Light uses a brighter ground with
    // slightly darker grid lines so they stay visible against it.
    private void ApplyViewportTheme()
    {
        if (ActualThemeVariant == ThemeVariant.Dark)
        {
            Scene.BackgroundColor = new Vector4(64 / 255.0f, 64 / 255.0f, 64 / 255.0f, 1.0f);
            Scene.GridColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
        }
        else
        {
            Scene.BackgroundColor = new Vector4(0.86f, 0.86f, 0.86f, 1.0f);
            Scene.GridColor = new Vector4(0.62f, 0.62f, 0.62f, 1.0f);
        }
    }

    #region Logging

    private void AppendLog(string tag, string message, System.Drawing.Color color)
    {
        // Called from any thread (often the background parse thread). Buffer the
        // line and ensure exactly one flush is queued onto the UI thread, where
        // it drains everything accumulated so far in a single pass. Posting at
        // Background priority lets a burst coalesce instead of posting per line.
        // (stdout is handled separately by the framework's console provider.)
        _pendingLogs.Enqueue((tag, message, color));
        if (Interlocked.Exchange(ref _logFlushQueued, 1) == 0)
        {
            Dispatcher.UIThread.Post(FlushLogs, DispatcherPriority.Background);
        }
    }

    // Drains every buffered line into the panel in one batch: a single
    // CollectionChanged storm the ListBox can absorb, one trim, and one scroll.
    private void FlushLogs()
    {
        // Reset the flag *before* draining: any line enqueued from here on queues
        // a fresh flush, while lines already enqueued are guaranteed to be drained
        // by the loop below. This is what makes the lock-free hand-off lossless.
        Interlocked.Exchange(ref _logFlushQueued, 0);

        LogLine last = null;
        while (_pendingLogs.TryDequeue(out var pending))
        {
            last = new LogLine(pending.Tag, pending.Message, BrushFor(pending.Color));
            _logLines.Add(last);
        }

        if (last is null)
        {
            return;
        }

        // Cap the panel so a heavy parse can't grow it without bound; drop the
        // oldest lines first.
        for (int over = _logLines.Count - MaxLogLines; over > 0; over--)
        {
            _logLines.RemoveAt(0);
        }

        // ScrollIntoView forces a synchronous layout pass, which can throw
        // "Invalid Arrange rectangle" from the virtualizing panel when the list
        // is churning hard (a heavy parse flushing back-to-back batches). The
        // auto-scroll is cosmetic, so never let a transient layout state abort
        // the app over it; skipping one scroll is harmless.
        try
        {
            logView.ScrollIntoView(last);
        }
        catch (InvalidOperationException)
        {
        }
    }

    // The tag colour comes from the logging provider as System.Drawing.Color;
    // cache the Avalonia brushes since only a handful of distinct colours occur.
    private IBrush BrushFor(System.Drawing.Color c)
    {
        uint key = (uint)c.ToArgb();
        if (!_brushCache.TryGetValue(key, out var brush))
        {
            brush = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
            _brushCache[key] = brush;
        }
        return brush;
    }

    #endregion

    #region File loading

    private async void OnLoadClick(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select CCS Files to load",
            AllowMultiple = true,
            FileTypeFilter = new[] { FileFilters.CCS, FileFilters.All },
        });

        LoadFiles(files.Select(f => f.Path.LocalPath));
    }

    private void LoadFiles(IEnumerable<string> fileNames)
    {
        List<string> paths = fileNames.ToList();
        if (paths.Count == 0)
        {
            return;
        }

        // Show the progress bar for this batch. ReportLoaded fires exactly once per
        // file as it reaches its terminal point - GL upload finished, or a parse
        // failure / null skip - so the bar advances only as files finish fully
        // loading and always reaches completion regardless of outcome.
        _vm.BeginLoading(paths.Count);

        // Parse off the UI thread so a large batch doesn't freeze the UI or stall
        // the render loop. Parsing is pure per-file CPU work with no shared state
        // (each CCSFile reads into its own buffers), so fan it across cores rather
        // than grinding through one file at a time on a single thread. The GL
        // upload (InitCCSFile) MUST run with the context current and against the
        // shared scene state, so it stays serialized on the render callback;
        // _glJobs is a concurrent queue, so many parse threads can enqueue safely.
        // Tree roots therefore appear in parse-completion order, not input order.
        Task.Run(() =>
        {
            Parallel.ForEach(paths, fileName =>
            {
                CCSFile file;
                try
                {
                    file = Scene.ReadCCSFile(fileName);
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("Failed to load {0}: {1}\n", fileName, ex.Message));
                    ReportFileLoaded();
                    return;
                }
                if (file == null)
                {
                    ReportFileLoaded();
                    return;
                }

                glViewport.EnqueueGlJob(() =>
                {
                    CCSTreeNode node = Scene.InitCCSFile(file);
                    if (node != null)
                    {
                        Dispatcher.UIThread.Post(() => _vm.CCSRoots.Add(node));
                    }
                    // Counts only here, after the GL upload completes, so a file is
                    // reported "loaded" only when it's genuinely ready in the scene.
                    ReportFileLoaded();
                });
            });
        });
    }

    // Marshals a single file-completion report onto the UI thread, where the
    // view-model's progress counts live. Called from the parse threads and the GL
    // render callback.
    private void ReportFileLoaded()
    {
        Dispatcher.UIThread.Post(_vm.ReportFileLoaded);
    }

    private void UnloadAllCCS()
    {
        // Free the GL resources on the render thread (context current), where the
        // job also clears the file list and scene state in one pass. The bound
        // trees are UI state, so clear them here on the UI thread.
        glViewport.EnqueueGlJob(() => Scene.UnloadAllCCSFiles());
        _vm.CCSRoots.Clear();
        _vm.AnimationsRoot.Nodes.Clear();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Drag & drop

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        var items = e.DataTransfer.TryGetFiles();
        if (items == null)
        {
            return;
        }

        LoadFiles(items.Select(i => i.Path.LocalPath));
    }

    #endregion

    #region Tree selection & context menus

    private void OnCCSTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CCSTreeNode node = ccsTree.SelectedItem as CCSTreeNode;
        TreeNodeTag tag = node?.Tag as TreeNodeTag;
        Scene.SelectedPreviewItemTag = tag;
        if (tag != null && tag.ObjectType == CCSFile.SECTION_ANIME)
        {
            var tmpAnime = tag.File.GetObject<CCSAnime>(tag.ObjectID);
            if (tmpAnime != null)
            {
                tmpAnime.HasEnded = false;
                tmpAnime.CurrentFrame = 0;
            }
        }
    }

    private void OnCCSTreeContextRequested(object sender, ContextRequestedEventArgs e)
    {
        var node = ContextNode(e);
        if (node == null)
        {
            return;
        }

        ccsTree.SelectedItem = node;
        OpenNodeMenu(e, ccsTree, BuildCCSNodeMenu(node));
    }

    private void OnSceneTreeContextRequested(object sender, ContextRequestedEventArgs e)
    {
        var node = ContextNode(e);
        if (node == null || node == _vm.AnimationsRoot)
        {
            return;
        }

        if (node.Tag is not TreeNodeTag tag || tag.ObjectType != CCSFile.SECTION_ANIME)
        {
            return;
        }

        OpenNodeMenu(e, sceneTree, Menu(MakeMenuItem("Remove", () =>
        {
            _vm.AnimationsRoot.Nodes.Remove(node);
            var anime = tag.File.GetObject<CCSAnime>(tag.ObjectID);
            if (anime != null)
            {
                Scene.RemoveAnime(anime);
            }
        })));
    }

    private ContextMenu BuildCCSNodeMenu(CCSTreeNode node)
    {
        if (node.Tag is not TreeNodeTag tag)
        {
            return null;
        }

        if (tag.Type == TreeNodeTag.NodeType.File)
        {
            return Menu(
                MakeMenuItem("Unload", () =>
                {
                    // DeInit() deletes GL resources, so run it on the render thread.
                    glViewport.EnqueueGlJob(() => Scene.UnloadCCSFile(tag.File));
                    _vm.CCSRoots.Remove(node);
                }),
                MakeMenuItem("Unload All", UnloadAllCCS),
                MakeMenuItem("View Info Report", () =>
                {
                    InfoWindow reportForm = new InfoWindow();
                    reportForm.SetReportText(tag.File.GetReport());
                    reportForm.Show();
                }));
        }

        if (tag.Type == TreeNodeTag.NodeType.Main && tag.ObjectType == CCSFile.SECTION_CLUMP)
        {
            return Menu(
                MakeMenuItem("Load Matrix...", () => LoadMatrix(tag)),
                MakeMenuItem("Edit Bones", () =>
                {
                    EditBoneWindow editFrm = new EditBoneWindow();
                    editFrm.SetClump(tag.File.GetObject<CCSClump>(tag.ObjectID));
                    editFrm.Show();
                }));
        }

        if (tag.Type == TreeNodeTag.NodeType.Main && tag.ObjectType == CCSFile.SECTION_ANIME)
        {
            return Menu(
                MakeMenuItem("Add to Scene", () =>
                {
                    var tmpAnime = tag.File.GetObject<CCSAnime>(tag.ObjectID);
                    if (tmpAnime != null)
                    {
                        Scene.AddAnime(tmpAnime);
                        _vm.AnimationsRoot.Nodes.Add(new CCSTreeNode(tmpAnime.ToNode().Text) { Tag = tag });
                    }
                }),
                MakeMenuItem("Set Pose", () =>
                {
                    var tmpAnime = tag.File.GetObject<CCSAnime>(tag.ObjectID);
                    tmpAnime?.FrameForward();
                }));
        }

        return null;
    }

    // ----- Context-menu helpers (shared by both trees) -----

    private static CCSTreeNode ContextNode(ContextRequestedEventArgs e)
    {
        return (e.Source as Control)?.DataContext as CCSTreeNode;
    }

    private static void OpenNodeMenu(ContextRequestedEventArgs e, Control owner, ContextMenu menu)
    {
        if (menu == null)
        {
            return;
        }

        e.Handled = true;
        menu.Placement = PlacementMode.Pointer;
        menu.Open(owner);
    }

    private static ContextMenu Menu(params MenuItem[] items)
    {
        ContextMenu menu = new ContextMenu();
        foreach (var mi in items)
        {
            ((IList)menu.Items).Add(mi);
        }

        return menu;
    }

    private static MenuItem MakeMenuItem(string header, Action action)
    {
        MenuItem mi = new MenuItem { Header = header };
        mi.Click += (_, _) => action();
        return mi;
    }

    private async void LoadMatrix(TreeNodeTag tag)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Binary File to load",
            AllowMultiple = false,
            FileTypeFilter = new[] { FileFilters.Bin, FileFilters.All },
        });
        var file = files.FirstOrDefault();
        if (file == null)
        {
            return;
        }

        var tmpClump = tag.File.GetObject<CCSClump>(tag.ObjectID);
        tmpClump?.LoadMatrixList(file.Path.LocalPath);
    }

    #endregion

    #region Mode toolbar

    // The grouped RadioButtons drive _vm.Mode (which writes Scene.SceneDisplay);
    // this only updates the panel layout, which can't bind cleanly because of the
    // GridLength column collapse.
    private void ApplyModeLayout(Scene.SceneMode mode)
    {
        bool leftVisible = mode != Scene.SceneMode.All;
        leftPanel.IsVisible = leftVisible;
        treeSplitter.IsVisible = leftVisible;
        splitGrid.ColumnDefinitions[0].Width = leftVisible ? new GridLength(240) : new GridLength(0);

        ccsTree.IsVisible = mode == Scene.SceneMode.Preview;
        scenePanel.IsVisible = mode == Scene.SceneMode.Scene;
    }

    #endregion

    #region Scene export

    private async void OnDumpObjClick(object sender, RoutedEventArgs e)
    {
        ExportToObjWindow dlg = new ExportToObjWindow();
        if (await dlg.ShowDialog<bool>(this))
        {
            Scene.DumpToObj(dlg.ExportPath, dlg.ExportCollision, dlg.SplitSubModels, dlg.SplitCollision, dlg.WithNormals, dlg.ExportDummies, dlg.DumpAnime);
        }
    }

    private async void OnDumpSmdClick(object sender, RoutedEventArgs e)
    {
        ExportToObjWindow dlg = new ExportToObjWindow();
        dlg.ConfigureForSmd();
        if (await dlg.ShowDialog<bool>(this))
        {
            Scene.DumpToSMD(dlg.ExportPath, dlg.WithNormals);
        }
    }

    // Exports the currently previewed animation (its posed clumps, one SMD per
    // frame) rather than the whole loaded scene. Ported from taarna23's fork.
    private async void OnDumpPreviewSmdClick(object sender, RoutedEventArgs e)
    {
        ExportToObjWindow dlg = new ExportToObjWindow();
        dlg.ConfigureForSmd();
        if (await dlg.ShowDialog<bool>(this))
        {
            Scene.DumpPreviewToSMD(dlg.ExportPath, dlg.WithNormals);
        }
    }

    #endregion

    #region Viewport input (forwarded to Scene)

    private void OnViewportPointerPressed(object sender, PointerPressedEventArgs e)
    {
        Control host = (Control)sender;
        host.Focus();
        var point = e.GetCurrentPoint(host);
        if (point.Properties.IsRightButtonPressed)
        {
            // Seed the last position so the first drag delta is zero (no jump).
            Scene.LastMouseX = (float)point.Position.X;
            Scene.LastMouseY = (float)point.Position.Y;
            e.Pointer.Capture(host);
        }
    }

    private void OnViewportPointerMoved(object sender, PointerEventArgs e)
    {
        Control host = (Control)sender;
        var point = e.GetCurrentPoint(host);
        Scene.MouseMove((float)point.Position.X, (float)point.Position.Y, point.Properties.IsRightButtonPressed);
    }

    private void OnViewportPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            e.Pointer.Capture(null);
        }
    }

    private void OnViewportPointerWheel(object sender, PointerWheelEventArgs e)
    {
        // Scene's zoom math is tuned for WinForms wheel units (~120 per notch);
        // Avalonia reports ~1.0 per notch. Negated so scrolling up zooms in.
        Scene.MouseWheel((float)(e.Delta.Y * -120.0));
    }

    private void OnViewportKeyDown(object sender, KeyEventArgs e)
    {
        Scene.KeyPress(MapCameraKey(e.Key), e.KeyModifiers.HasFlag(KeyModifiers.Shift), e.KeyModifiers.HasFlag(KeyModifiers.Control));
    }

    private void OnViewportKeyUp(object sender, KeyEventArgs e)
    {
        Scene.KeyRelease(MapCameraKey(e.Key), e.KeyModifiers.HasFlag(KeyModifiers.Shift), e.KeyModifiers.HasFlag(KeyModifiers.Control));
    }

    private static Scene.CameraKey MapCameraKey(Key k)
    {
        switch (k)
        {
            case Key.W: return Scene.CameraKey.Forward;
            case Key.S: return Scene.CameraKey.Backward;
            case Key.A: return Scene.CameraKey.Left;
            case Key.D: return Scene.CameraKey.Right;
            case Key.X: return Scene.CameraKey.Up;
            case Key.Z: return Scene.CameraKey.Down;
            case Key.OemPlus:
            case Key.Add: return Scene.CameraKey.ZoomIn;
            case Key.OemMinus:
            case Key.Subtract: return Scene.CameraKey.ZoomOut;
            default: return Scene.CameraKey.None;
        }
    }

    #endregion
}
