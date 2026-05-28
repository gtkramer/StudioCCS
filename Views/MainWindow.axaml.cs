using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using StudioCCS.Controls;
using StudioCCS.libCCS;
// 'Grid' is ambiguous: StudioCCS.Grid (the GL grid renderer) shadows
// Avalonia.Controls.Grid within this nested namespace, so alias the control.
using AvGrid = Avalonia.Controls.Grid;

namespace StudioCCS.Views
{
    public partial class MainWindow : Window
    {
        private TreeView _ccsTree;
        private TreeView _sceneTree;
        private TreeViewItem _sceneAnimationNode;
        private TextBox _logView;
        private TextBlock _lblRenderMode;
        private TextBlock _lblCamera;
        private Panel _leftPanel;
        private AvGrid _scenePanel;
        private AvGrid _splitGrid;
        private Control _treeSplitter;
        private ToggleButton _tbtnPreview, _tbtnScene, _tbtnAll;
        private GlViewport _glViewport;

        private bool _suppressModeEvents;
        private readonly DispatcherTimer _statusTimer;

        public MainWindow() : this(null)
        {
        }

        public MainWindow(string[] startupFiles)
        {
            InitializeComponent();

            _glViewport = this.FindControl<GlViewport>("glViewport");
            _ccsTree = this.FindControl<TreeView>("ccsTree");
            _sceneTree = this.FindControl<TreeView>("sceneTree");
            _logView = this.FindControl<TextBox>("logView");
            _lblRenderMode = this.FindControl<TextBlock>("lblRenderMode");
            _lblCamera = this.FindControl<TextBlock>("lblCamera");
            _leftPanel = this.FindControl<Panel>("leftPanel");
            _scenePanel = this.FindControl<AvGrid>("scenePanel");
            _splitGrid = this.FindControl<AvGrid>("splitGrid");
            _treeSplitter = this.FindControl<Control>("treeSplitter");
            _tbtnPreview = this.FindControl<ToggleButton>("tbtnPreview");
            _tbtnScene = this.FindControl<ToggleButton>("tbtnScene");
            _tbtnAll = this.FindControl<ToggleButton>("tbtnAll");

            // Scene animations root node.
            _sceneAnimationNode = new TreeViewItem { Header = "Animations" };
            ((IList)_sceneTree.Items).Add(_sceneAnimationNode);

            // Route Logger output to the log panel (marshalled onto the UI thread).
            Logger.SetOutput(AppendLog);

            // Initial render-option menu states (matches the original defaults).
            SetCheck("miTextured", true);
            SetCheck("miGrid", true);
            SetCheck("miCollision", true);
            SetCheck("miDummies", true);
            SetCheck("miLights", true);
            SetCheck("miAxisViewport", true);
            SetCheck("miWorldCenter", true);
            ApplyViewMenu();

            // Drag & drop CCS files onto the window.
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);

            // SMD export was a debug-only feature in the original (gated behind #if DEBUG).
#if DEBUG
            this.FindControl<MenuItem>("miDumpSmd").IsVisible = true;
#endif

            SetMode(Scene.SceneMode.Preview);

            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _statusTimer.Tick += (_, _) => UpdateStatus();
            _statusTimer.Start();

            // Optional files passed on the command line (e.g. "open with").
            if (startupFiles != null)
            {
                LoadFiles(startupFiles.Where(File.Exists));
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
            _logView.Text += text;
            _logView.CaretIndex = _logView.Text.Length;
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
            // resulting tree node is then marshalled back to the UI thread.
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

                    _glViewport.EnqueueGlJob(() =>
                    {
                        CcsTreeNode node = Scene.InitCCSFile(file);
                        if (node != null)
                        {
                            Dispatcher.UIThread.Post(() => ((IList)_ccsTree.Items).Add(BuildTreeItem(node)));
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

        #region Tree building & context menus

        private TreeViewItem BuildTreeItem(CcsTreeNode node)
        {
            var item = new TreeViewItem { Header = node.Text, Tag = node.Tag };

            if (node.Tag is TreeNodeTag tag)
            {
                var menu = BuildContextMenu(tag, item);
                if (menu != null) item.ContextMenu = menu;
            }

            foreach (var child in node.Nodes)
            {
                ((IList)item.Items).Add(BuildTreeItem(child));
            }
            return item;
        }

        private ContextMenu BuildContextMenu(TreeNodeTag tag, TreeViewItem item)
        {
            var items = new List<MenuItem>();

            if (tag.Type == TreeNodeTag.NodeType.File)
            {
                items.Add(MakeMenuItem("Unload", () =>
                {
                    // DeInit() deletes GL resources, so run it on the render thread.
                    _glViewport.EnqueueGlJob(() => Scene.UnloadCCSFile(tag.File));
                    ((IList)_ccsTree.Items).Remove(item);
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
                            ((IList)_sceneAnimationNode.Items).Add(BuildSceneAnimeItem(tmpAnime, tag));
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

        private TreeViewItem BuildSceneAnimeItem(CCSAnime anime, TreeNodeTag tag)
        {
            var item = new TreeViewItem
            {
                Header = anime.ToNode().Text,
                Tag = tag,
            };
            var menu = new ContextMenu();
            ((IList)menu.Items).Add(MakeMenuItem("Remove", () =>
            {
                ((IList)_sceneAnimationNode.Items).Remove(item);
                Scene.RemoveAnime(anime);
            }));
            item.ContextMenu = menu;
            return item;
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

        private void OnCcsTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = _ccsTree.SelectedItem as TreeViewItem;
            var tag = item?.Tag as TreeNodeTag;
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

        #endregion

        #region View / render option menus

        private void SetCheck(string name, bool value)
        {
            var mi = this.FindControl<MenuItem>(name);
            if (mi != null) mi.IsChecked = value;
        }

        private bool IsChecked(string name)
        {
            var mi = this.FindControl<MenuItem>(name);
            return mi != null && mi.IsChecked;
        }

        private void OnRenderToggle(object sender, RoutedEventArgs e)
        {
            ApplyViewMenu();
        }

        private void ApplyViewMenu()
        {
            Scene.DrawWireframe = IsChecked("miWireframe");
            Scene.DrawVertexColors = IsChecked("miVertexColors");
            Scene.DrawVertexNormals = IsChecked("miVertexNormals");
            Scene.DrawTextures = IsChecked("miTextured");
            Scene.BackfaceCull = IsChecked("miBackface");
            Scene.DrawViewGrid = IsChecked("miGrid");
            Scene.DrawCollisionMeshes = IsChecked("miCollision");
            Scene.DrawDummyHelpers = IsChecked("miDummies");
            Scene.DrawLightHelpers = IsChecked("miLights");
            Scene.DrawViewAxis = IsChecked("miAxisViewport");
            Scene.DrawWorldCenter = IsChecked("miWorldCenter");
        }

        private void OnAxisMovementToggle(object sender, RoutedEventArgs e)
        {
            Scene.DefaultToAxisMovement = IsChecked("miAxisMovement");
        }

        #endregion

        #region Mode toolbar

        private void OnModeClick(object sender, RoutedEventArgs e)
        {
            if (_suppressModeEvents) return;
            if (sender == _tbtnPreview) SetMode(Scene.SceneMode.Preview);
            else if (sender == _tbtnScene) SetMode(Scene.SceneMode.Scene);
            else if (sender == _tbtnAll) SetMode(Scene.SceneMode.All);
        }

        private void SetMode(Scene.SceneMode mode)
        {
            _suppressModeEvents = true;
            _tbtnPreview.IsChecked = mode == Scene.SceneMode.Preview;
            _tbtnScene.IsChecked = mode == Scene.SceneMode.Scene;
            _tbtnAll.IsChecked = mode == Scene.SceneMode.All;
            _suppressModeEvents = false;

            Scene.SceneDisplay = mode;

            bool leftVisible = mode != Scene.SceneMode.All;
            _leftPanel.IsVisible = leftVisible;
            _treeSplitter.IsVisible = leftVisible;
            _splitGrid.ColumnDefinitions[0].Width = leftVisible ? new GridLength(240) : new GridLength(0);

            _ccsTree.IsVisible = mode == Scene.SceneMode.Preview;
            _scenePanel.IsVisible = mode == Scene.SceneMode.Scene;
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

        #region Status bar

        private void UpdateStatus()
        {
            ArcBallCamera cam = Scene.CurrentCamera();
            _lblCamera.Text = string.Format(
                "Camera: Rotation: {0}, {1}, {2}, Target: {3}, {4}, {5}, Distance: {6}",
                cam.Rotation.X, cam.Rotation.Y, cam.Rotation.Z,
                cam.Target.X, cam.Target.Y, cam.Target.Z, cam.Distance);

            _lblRenderMode.Text = RenderModeText();
        }

        private string RenderModeText()
        {
            if ((Scene.GetRenderMode() & 15) == 0) return "None";

            var options = new List<string>();
            if (Scene.DrawWireframe) options.Add("Wireframe");
            if (Scene.DrawVertexColors) options.Add("Vertex Colors");
            if (Scene.DrawVertexNormals) options.Add("Vertex Normals");
            if (Scene.DrawTextures) options.Add("Textured");

            string text = string.Join("/", options);
            text += Scene.BackfaceCull ? " (Backface Culling)" : " (No Backface Culling)";
            return text;
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
