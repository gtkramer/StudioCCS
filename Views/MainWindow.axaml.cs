using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using StudioCCS.libCCS;
using StudioCCS.ViewModels;

namespace StudioCCS.Views
{
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

            // Drag & drop CCS files onto the window.
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);

            // SMD export was a debug-only feature in the original (gated behind #if DEBUG).
#if DEBUG
            miDumpSmd.IsVisible = true;
#endif

            // The toolbar RadioButtons set _vm.Mode; mirror mode changes into the
            // panel layout (and apply the initial layout, since the VM's default
            // Mode is set before this handler is attached).
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.Mode)) ApplyModeLayout(_vm.Mode);
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

        #region Logging

        private void AppendLog(string tag, string message, System.Drawing.Color color)
        {
            // Logging can originate on the background parse thread; marshal to the UI
            // thread first so the list mutation happens on the UI thread. (stdout is
            // handled separately by the framework's console provider.)
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => AppendLog(tag, message, color));
                return;
            }

            var line = new LogLine(tag, message, BrushFor(color));
            _logLines.Add(line);
            if (_logLines.Count > MaxLogLines) _logLines.RemoveAt(0);
            logView.ScrollIntoView(line);
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
                FileTypeFilter = new[] { FileFilters.Ccs, FileFilters.All },
            });

            LoadFiles(files.Select(f => f.Path.LocalPath));
        }

        private void LoadFiles(IEnumerable<string> fileNames)
        {
            var paths = fileNames.ToList();
            if (paths.Count == 0) return;

            // Parse on a background thread so a large file doesn't freeze the UI or
            // stall the render loop. The GL upload (InitCCSFile) MUST run with the
            // context current, so it's enqueued onto the render callback; the
            // resulting node is then added to the bound collection on the UI thread.
            Task.Run(() =>
            {
                foreach (var fileName in paths)
                {
                    CCSFile file;
                    try
                    {
                        file = Scene.ReadCCSFile(fileName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("Failed to load {0}: {1}\n", fileName, ex.Message));
                        continue;
                    }
                    if (file == null) continue;

                    glViewport.EnqueueGlJob(() =>
                    {
                        CcsTreeNode node = Scene.InitCCSFile(file);
                        if (node != null)
                        {
                            Dispatcher.UIThread.Post(() => _vm.CcsRoots.Add(node));
                        }
                    });
                }
            });
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
            if (items == null) return;
            LoadFiles(items.Select(i => i.Path.LocalPath));
        }

        #endregion

        #region Tree selection & context menus

        private void OnCcsTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var node = ccsTree.SelectedItem as CcsTreeNode;
            var tag = node?.Tag as TreeNodeTag;
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

        private void OnCcsTreeContextRequested(object sender, ContextRequestedEventArgs e)
        {
            var node = ContextNode(e);
            if (node == null) return;

            ccsTree.SelectedItem = node;
            OpenNodeMenu(e, ccsTree, BuildCcsNodeMenu(node));
        }

        private void OnSceneTreeContextRequested(object sender, ContextRequestedEventArgs e)
        {
            var node = ContextNode(e);
            if (node == null || node == _vm.AnimationsRoot) return;
            if (node.Tag is not TreeNodeTag tag || tag.ObjectType != CCSFile.SECTION_ANIME) return;

            OpenNodeMenu(e, sceneTree, Menu(MakeMenuItem("Remove", () =>
            {
                _vm.AnimationsRoot.Nodes.Remove(node);
                var anime = tag.File.GetObject<CCSAnime>(tag.ObjectID);
                if (anime != null) Scene.RemoveAnime(anime);
            })));
        }

        private ContextMenu BuildCcsNodeMenu(CcsTreeNode node)
        {
            if (node.Tag is not TreeNodeTag tag) return null;

            if (tag.Type == TreeNodeTag.NodeType.File)
            {
                return Menu(
                    MakeMenuItem("Unload", () =>
                    {
                        // DeInit() deletes GL resources, so run it on the render thread.
                        glViewport.EnqueueGlJob(() => Scene.UnloadCCSFile(tag.File));
                        _vm.CcsRoots.Remove(node);
                    }),
                    MakeMenuItem("View Info Report", () =>
                    {
                        var reportForm = new InfoWindow();
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
                        var editFrm = new EditBoneWindow();
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
                            _vm.AnimationsRoot.Nodes.Add(new CcsTreeNode(tmpAnime.ToNode().Text) { Tag = tag });
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

        private static CcsTreeNode ContextNode(ContextRequestedEventArgs e)
        {
            return (e.Source as Control)?.DataContext as CcsTreeNode;
        }

        private static void OpenNodeMenu(ContextRequestedEventArgs e, Control owner, ContextMenu menu)
        {
            if (menu == null) return;
            e.Handled = true;
            menu.Placement = PlacementMode.Pointer;
            menu.Open(owner);
        }

        private static ContextMenu Menu(params MenuItem[] items)
        {
            var menu = new ContextMenu();
            foreach (var mi in items) ((IList)menu.Items).Add(mi);
            return menu;
        }

        private static MenuItem MakeMenuItem(string header, Action action)
        {
            var mi = new MenuItem { Header = header };
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
            if (file == null) return;

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
            var dlg = new ExportToObjWindow();
            if (await dlg.ShowDialog<bool>(this))
            {
                Scene.DumpToObj(dlg.ExportPath, dlg.ExportCollision, dlg.SplitSubModels, dlg.SplitCollision, dlg.WithNormals, dlg.ExportDummies, dlg.DumpAnime);
            }
        }

        private async void OnDumpSmdClick(object sender, RoutedEventArgs e)
        {
            var dlg = new ExportToObjWindow();
            dlg.ConfigureForSmd();
            if (await dlg.ShowDialog<bool>(this))
            {
                Scene.DumpToSMD(dlg.ExportPath, dlg.WithNormals);
            }
        }

        #endregion

        #region Viewport input (forwarded to Scene)

        private void OnViewportPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var host = (Control)sender;
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
            var host = (Control)sender;
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
}
