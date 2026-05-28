using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using StudioCCS.libCCS;
using StudioCCS.ViewModels;

namespace StudioCCS.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new MainViewModel();
        private bool _suppressModeEvents;
        private readonly DispatcherTimer _statusTimer;

        public MainWindow() : this(null)
        {
        }

        public MainWindow(string[] startupFiles)
        {
            InitializeComponent();
            DataContext = _vm;

            // Route Logger output to the log panel (marshalled onto the UI thread).
            Logger.SetOutput(AppendLog);

            // Drag & drop CCS files onto the window.
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);

            // SMD export was a debug-only feature in the original (gated behind #if DEBUG).
#if DEBUG
            miDumpSmd.IsVisible = true;
#endif

            SetMode(Scene.SceneMode.Preview);

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

        private void AppendLog(string text, System.Drawing.Color color)
        {
            // Logging can originate on the background parse thread; marshal to the UI
            // thread first so the console echo and TextBox update happen exactly once.
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => AppendLog(text, color));
                return;
            }

            Console.Write(text);
            logView.Text += text;
            logView.CaretIndex = logView.Text.Length;
        }

        #endregion

        #region File loading

        private async void OnLoadClick(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select CCS Files to load",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("CCS Files") { Patterns = new[] { "*.ccs", "*.tmp" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
                },
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
                        Logger.LogError(string.Format("Failed to load {0}: {1}\n", fileName, ex.Message));
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
            var node = (e.Source as Control)?.DataContext as CcsTreeNode;
            if (node == null) return;

            ccsTree.SelectedItem = node;
            var menu = BuildContextMenu(node);
            if (menu == null) return;

            e.Handled = true;
            menu.Placement = PlacementMode.Pointer;
            menu.Open(ccsTree);
        }

        private void OnSceneTreeContextRequested(object sender, ContextRequestedEventArgs e)
        {
            var node = (e.Source as Control)?.DataContext as CcsTreeNode;
            if (node == null || node == _vm.AnimationsRoot) return;
            if (!(node.Tag is TreeNodeTag tag) || tag.ObjectType != CCSFile.SECTION_ANIME) return;

            var menu = new ContextMenu();
            ((IList)menu.Items).Add(MakeMenuItem("Remove", () =>
            {
                _vm.AnimationsRoot.Nodes.Remove(node);
                var anime = tag.File.GetObject<CCSAnime>(tag.ObjectID);
                if (anime != null) Scene.RemoveAnime(anime);
            }));

            e.Handled = true;
            menu.Placement = PlacementMode.Pointer;
            menu.Open(sceneTree);
        }

        private ContextMenu BuildContextMenu(CcsTreeNode node)
        {
            if (!(node.Tag is TreeNodeTag tag)) return null;

            var items = new List<MenuItem>();
            if (tag.Type == TreeNodeTag.NodeType.File)
            {
                items.Add(MakeMenuItem("Unload", () =>
                {
                    // DeInit() deletes GL resources, so run it on the render thread.
                    glViewport.EnqueueGlJob(() => Scene.UnloadCCSFile(tag.File));
                    _vm.CcsRoots.Remove(node);
                }));
                items.Add(MakeMenuItem("View Info Report", () =>
                {
                    var reportForm = new InfoWindow();
                    reportForm.SetReportText(tag.File.GetReport());
                    reportForm.Show();
                }));
            }
            else if (tag.Type == TreeNodeTag.NodeType.Main)
            {
                if (tag.ObjectType == CCSFile.SECTION_CLUMP)
                {
                    items.Add(MakeMenuItem("Load Matrix...", () => LoadMatrix(tag)));
                    items.Add(MakeMenuItem("Edit Bones", () =>
                    {
                        var editFrm = new EditBoneWindow();
                        editFrm.SetClump(tag.File.GetObject<CCSClump>(tag.ObjectID));
                        editFrm.Show();
                    }));
                }
                else if (tag.ObjectType == CCSFile.SECTION_ANIME)
                {
                    items.Add(MakeMenuItem("Add to Scene", () =>
                    {
                        var tmpAnime = tag.File.GetObject<CCSAnime>(tag.ObjectID);
                        if (tmpAnime != null)
                        {
                            Scene.AddAnime(tmpAnime);
                            _vm.AnimationsRoot.Nodes.Add(new CcsTreeNode(tmpAnime.ToNode().Text) { Tag = tag });
                        }
                    }));
                    items.Add(MakeMenuItem("Set Pose", () =>
                    {
                        var tmpAnime = tag.File.GetObject<CCSAnime>(tag.ObjectID);
                        tmpAnime?.FrameForward();
                    }));
                }
            }

            if (items.Count == 0) return null;
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
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Bin Files") { Patterns = new[] { "*.bin" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
                },
            });
            var file = files.FirstOrDefault();
            if (file == null) return;

            var tmpClump = tag.File.GetObject<CCSClump>(tag.ObjectID);
            tmpClump?.LoadMatrixList(file.Path.LocalPath);
        }

        #endregion

        #region Mode toolbar

        private void OnModeClick(object sender, RoutedEventArgs e)
        {
            if (_suppressModeEvents) return;
            if (sender == tbtnPreview) SetMode(Scene.SceneMode.Preview);
            else if (sender == tbtnScene) SetMode(Scene.SceneMode.Scene);
            else if (sender == tbtnAll) SetMode(Scene.SceneMode.All);
        }

        private void SetMode(Scene.SceneMode mode)
        {
            _suppressModeEvents = true;
            tbtnPreview.IsChecked = mode == Scene.SceneMode.Preview;
            tbtnScene.IsChecked = mode == Scene.SceneMode.Scene;
            tbtnAll.IsChecked = mode == Scene.SceneMode.All;
            _suppressModeEvents = false;

            Scene.SceneDisplay = mode;

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
            // Avalonia reports ~1.0 per notch, so scale up to match.
            Scene.MouseWheel((float)(e.Delta.Y * 120.0));
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
