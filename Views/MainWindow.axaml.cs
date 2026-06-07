using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

    // The log panel's backing store. Owns the line buffer and the thread-safe,
    // coalesced hand-off from the (many) logging threads to the UI thread — every
    // threading concern about the panel lives in LogConsoleModel, not here.
    private readonly LogConsoleModel _log = new LogConsoleModel();

    // The log ListBox's inner ScrollViewer, resolved once its template is applied
    // (see HookLogScroll). We tail it to the newest line ourselves by setting its
    // offset; _logPinnedToBottom records whether the user is parked at the bottom
    // (so we keep tailing) or has scrolled up to read (so we leave them be).
    private ScrollViewer _logScroll;
    private bool _logPinnedToBottom = true;

    public MainWindow() : this(null)
    {
    }

    public MainWindow(string[] startupFiles)
    {
        InitializeComponent();
        DataContext = _vm;

        logView.ItemsSource = _log.Lines;

        // Configure logging: the framework owns levels/filtering and the stdout
        // sink; our custom provider routes to the log panel. _log.Append is
        // thread-safe and marshals onto the UI thread itself, so the provider can
        // hand it lines from any thread.
        Log.Init(b => b
            .SetMinimumLevel(LogLevel.Information)
            .AddConsole(o => o.FormatterName = CompactConsoleFormatter.FormatterName)
            .AddConsoleFormatter<CompactConsoleFormatter, ConsoleFormatterOptions>()
            .AddProvider(new PanelLoggerProvider(_log.Append)));

        // The log ListBox's ScrollViewer only exists once the control is
        // templated and attached, so wire our auto-tail after the window loads.
        Loaded += (_, _) => HookLogScroll();

        // Let a whole tree row toggle expand/collapse, not just the chevron.
        ccsTree.Tapped += TreeViewExpand.ToggleOnTap;
        sceneTree.Tapped += TreeViewExpand.ToggleOnTap;

        // The GL viewport's clear/grid colours aren't styled by the theme engine
        // (they're set in raw GL), so push theme-appropriate values into Scene
        // now and again whenever the OS light/dark setting changes. Scene re-reads
        // these every frame, so the switch is reflected immediately.
        ActualThemeVariantChanged += (_, _) => ApplyViewportTheme();
        ApplyViewportTheme();

        // A KeyUp only reaches the focused control, so a camera key held while the
        // window loses activation (switching windows, alt-tab) would never be
        // released and would strand the render loop in continuous redraw. Drop all
        // held keys on deactivation; the viewport Border does the same on LostFocus
        // for focus moves within the window.
        Deactivated += (_, _) => Scene.ReleaseAllCameraKeys();

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

            // Wake the viewport for any view-model change. Everything here is
            // either render-affecting (a View-menu toggle, the mode, a
            // write-through option) or a status string that only fires while
            // something is already happening - the camera readout changes only as
            // the camera moves, the load counters only during a load - and all of
            // those are silent at rest. So a blanket redraw is safe and means no
            // option toggle can be forgotten. (See Scene's render-on-demand.)
            Scene.RequestRedraw();
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

        Scene.RequestRedraw();
    }

    #region Log panel auto-scroll

    // The log "tails" the newest line like a terminal: as lines arrive we keep
    // the view pinned to the bottom, unless the user has scrolled up to read
    // something — then we leave the view put until they scroll back down. We do
    // this by setting the ScrollViewer's offset, never via ScrollIntoView: that
    // method forces a synchronous layout pass through the virtualizing panel,
    // which is what used to throw "Invalid Arrange rectangle" and (once that was
    // swallowed) leave the tail rows arranged on top of each other. Setting the
    // offset is a plain property change the panel resolves on its normal layout
    // pass, so the whole class of crash/corruption is gone, not suppressed.

    // Resolves the ListBox's inner ScrollViewer and subscribes once the template
    // is applied. Auto-tail simply no-ops if it can't be found, so a template
    // change can never turn this into a crash.
    private void HookLogScroll()
    {
        if (_logScroll != null)
        {
            return;
        }

        _logScroll = logView.FindDescendantOfType<ScrollViewer>();
        if (_logScroll == null)
        {
            return;
        }

        _logScroll.ScrollChanged += OnLogScrollChanged;
        _logScroll.LayoutUpdated += OnLogLayoutUpdated;
    }

    // Re-evaluate the pin only on a genuine offset move (the user scrolling, or
    // our own tail). A flushed batch grows the extent without moving the offset,
    // so excluding extent-only changes here is what stops newly-arrived lines
    // from being mistaken for the user scrolling away and unpinning us.
    private void OnLogScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.OffsetDelta.Y != 0)
        {
            _logPinnedToBottom = IsLogAtBottom();
        }
    }

    // After any layout that grew the extent (e.g. a flushed batch added lines),
    // glue the view back to the newest line — but only while pinned, so a user
    // who has scrolled up keeps their place. The guard makes this a cheap no-op
    // on the layout passes where nothing moved the bottom.
    private void OnLogLayoutUpdated(object sender, EventArgs e)
    {
        if (!_logPinnedToBottom || _logScroll == null)
        {
            return;
        }

        double maxY = Math.Max(0, _logScroll.Extent.Height - _logScroll.Viewport.Height);
        if (Math.Abs(_logScroll.Offset.Y - maxY) > 0.5)
        {
            _logScroll.Offset = new Vector(_logScroll.Offset.X, maxY);
        }
    }

    // "At the bottom" with a one-pixel tolerance so sub-pixel rounding on the
    // last row doesn't read as scrolled-up.
    private bool IsLogAtBottom()
    {
        double maxY = _logScroll.Extent.Height - _logScroll.Viewport.Height;
        return _logScroll.Offset.Y >= maxY - 1.0;
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

        Scene.RequestRedraw();
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
                    Scene.RequestRedraw();
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
        try
        {
            tmpClump?.LoadMatrixList(file.Path.LocalPath);
        }
        catch (Exception ex)
        {
            Log.Error(string.Format("Failed to load matrix from {0}: {1}\n", file.Path.LocalPath, ex.Message));
        }

        Scene.RequestRedraw();
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
            // Read the dialog's control-backed values here on the UI thread; the
            // export runs on a background thread and must never touch its controls.
            string path = dlg.ExportPath;
            bool collision = dlg.ExportCollision;
            bool splitSub = dlg.SplitSubModels;
            bool splitCollision = dlg.SplitCollision;
            bool normals = dlg.WithNormals;
            bool dummies = dlg.ExportDummies;
            bool anime = dlg.DumpAnime;
            await RunExport("OBJ", () => Scene.DumpToObj(path, collision, splitSub, splitCollision, normals, dummies, anime));
        }
    }

    private async void OnDumpSmdClick(object sender, RoutedEventArgs e)
    {
        ExportToObjWindow dlg = new ExportToObjWindow();
        dlg.ConfigureForSmd();
        if (await dlg.ShowDialog<bool>(this))
        {
            // Snapshot the dialog's control values on the UI thread (see OnDumpObjClick).
            string path = dlg.ExportPath;
            bool normals = dlg.WithNormals;
            await RunExport("SMD", () => Scene.DumpToSMD(path, normals));
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
            // Snapshot the dialog's control values on the UI thread (see OnDumpObjClick).
            string path = dlg.ExportPath;
            bool normals = dlg.WithNormals;
            await RunExport("preview SMD", () => Scene.DumpPreviewToSMD(path, normals));
        }
    }

    // Runs a scene export off the UI thread so a large export no longer freezes
    // the app. The dump mutates shared scene state (each clump's FrameForward
    // advances/recomputes its pose) that the render loop reads every frame, so for
    // the duration we suspend rendering (glViewport skips all scene work) and put
    // up a modal busy dialog. The modality is load-bearing, not just feedback: it
    // blocks the menu/tree actions that would otherwise mutate the scene from the
    // UI thread while the background export reads it. Any I/O failure is logged
    // rather than crashing this async-void caller.
    private async Task RunExport(string what, Action export)
    {
        BusyDialog busy = new BusyDialog();
        busy.SetMessage(string.Format("Exporting {0}...", what));

        // Raise the guard and show the modal inside the try so the finally always
        // clears the guard and closes the dialog - even if ShowDialog throws -
        // rather than leaving the viewport suspended with no way to recover.
        try
        {
            Scene.ExportInProgress = true;
            _ = busy.ShowDialog(this);
            await Task.Run(export);
        }
        catch (Exception ex)
        {
            Log.Error(string.Format("Export failed: {0}\n", ex.Message));
        }
        finally
        {
            Scene.ExportInProgress = false;
            Scene.RequestRedraw();
            busy.AllowClose();
            busy.Close();
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

    private void OnViewportLostFocus(object sender, RoutedEventArgs e)
    {
        // Focus moved off the viewport (e.g. to the tree), so its KeyUp events stop
        // arriving; release any held camera keys so the render loop can return to
        // idle rather than spinning on a key that will never report up.
        Scene.ReleaseAllCameraKeys();
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
            case Key.R: return Scene.CameraKey.Reset;
            case Key.OemPlus:
            case Key.Add: return Scene.CameraKey.ZoomIn;
            case Key.OemMinus:
            case Key.Subtract: return Scene.CameraKey.ZoomOut;
            default: return Scene.CameraKey.None;
        }
    }

    #endregion
}
