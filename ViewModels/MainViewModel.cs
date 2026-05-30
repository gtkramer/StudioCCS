using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StudioCCS.ViewModels
{
    /// <summary>
    /// A light view-model exposing the bindable bits of the main window: the tree
    /// data sources, the render-option toggles (View menu), and the status-bar text.
    /// It is intentionally a thin shim over the imperative static <see cref="Scene"/>
    /// — the render options write straight through to it — rather than a full MVVM
    /// layer, which would add ceremony with no benefit for this kind of app.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        // Tree data sources bound to the TreeViews (the templates recurse Nodes).
        public ObservableCollection<CcsTreeNode> CcsRoots { get; } = new ObservableCollection<CcsTreeNode>();
        public CcsTreeNode AnimationsRoot { get; } = new CcsTreeNode("Animations");
        public ObservableCollection<CcsTreeNode> SceneRoots { get; }

        public MainViewModel()
        {
            SceneRoots = new ObservableCollection<CcsTreeNode> { AnimationsRoot };

            // Initial render-option state (matches the original defaults).
            Textured = true;
            DrawGrid = true;
            DrawCollisionMeshes = true;
            DrawBoundingBoxes = true;
            DrawDummies = true;
            DrawLightHelpers = true;
            DrawAxisViewport = true;
            DrawWorldCenter = true;
            UpdateRenderModeStatus();
        }

        #region Scene mode

        // Single source of truth for the viewport mode. The three Is*Mode wrappers
        // exist so the toolbar's grouped RadioButtons can two-way bind to bools; the
        // RadioButton group handles mutual exclusion, and Mode writes through to Scene.
        private Scene.SceneMode _mode = Scene.SceneMode.Preview;
        public Scene.SceneMode Mode
        {
            get => _mode;
            set
            {
                if (SetField(ref _mode, value))
                {
                    Scene.SceneDisplay = value;
                    OnPropertyChanged(nameof(IsPreviewMode));
                    OnPropertyChanged(nameof(IsSceneMode));
                    OnPropertyChanged(nameof(IsAllMode));
                }
            }
        }

        public bool IsPreviewMode
        {
            get => Mode == Scene.SceneMode.Preview;
            set
            {
                if (value)
                {
                    Mode = Scene.SceneMode.Preview;
                }
            }
        }

        public bool IsSceneMode
        {
            get => Mode == Scene.SceneMode.Scene;
            set
            {
                if (value)
                {
                    Mode = Scene.SceneMode.Scene;
                }
            }
        }

        public bool IsAllMode
        {
            get => Mode == Scene.SceneMode.All;
            set
            {
                if (value)
                {
                    Mode = Scene.SceneMode.All;
                }
            }
        }

        #endregion

        #region Status bar

        private string _cameraStatus = "";
        public string CameraStatus
        {
            get => _cameraStatus;
            private set => SetField(ref _cameraStatus, value);
        }

        private string _renderModeStatus = "";
        public string RenderModeStatus
        {
            get => _renderModeStatus;
            private set => SetField(ref _renderModeStatus, value);
        }

        /// <summary>Pulls the current camera state from the Scene (called on a timer).</summary>
        public void RefreshCameraStatus()
        {
            ArcBallCamera cam = Scene.CurrentCamera();
            // Fixed-width fields (monospace status bar) keep the readout from jittering as it
            // refreshes ~10x/sec; grouped labels make each axis legible at a glance.
            CameraStatus = string.Format(
                "Rot {0,6:0.#}° {1,6:0.#}° {2,6:0.#}°     Target {3,5:0.#} {4,5:0.#} {5,5:0.#}     Dist {6,5:0.#}",
                cam.Rotation.X, cam.Rotation.Y, cam.Rotation.Z,
                cam.Target.X, cam.Target.Y, cam.Target.Z, cam.Distance);
        }

        private void UpdateRenderModeStatus()
        {
            var options = new List<string>();
            if (_wireframe)
            {
                options.Add("Wireframe");
            }

            if (_vertexColors)
            {
                options.Add("Vertex Colors");
            }

            if (_vertexNormals)
            {
                options.Add("Vertex Normals");
            }

            if (_textured)
            {
                options.Add("Textured");
            }

            if (options.Count == 0)
            {
                RenderModeStatus = "None";
                return;
            }

            string text = string.Join("/", options);
            text += _backfaceCull ? " (Backface Culling)" : " (No Backface Culling)";
            RenderModeStatus = text;
        }

        #endregion

        #region Render options (write through to Scene)

        private bool _wireframe;
        public bool Wireframe
        {
            get => _wireframe;
            set { if (SetField(ref _wireframe, value)) { Scene.DrawWireframe = value; UpdateRenderModeStatus(); } }
        }

        private bool _vertexColors;
        public bool VertexColors
        {
            get => _vertexColors;
            set { if (SetField(ref _vertexColors, value)) { Scene.DrawVertexColors = value; UpdateRenderModeStatus(); } }
        }

        private bool _vertexNormals;
        public bool VertexNormals
        {
            get => _vertexNormals;
            set { if (SetField(ref _vertexNormals, value)) { Scene.DrawVertexNormals = value; UpdateRenderModeStatus(); } }
        }

        private bool _textured;
        public bool Textured
        {
            get => _textured;
            set { if (SetField(ref _textured, value)) { Scene.DrawTextures = value; UpdateRenderModeStatus(); } }
        }

        private bool _backfaceCull;
        public bool BackfaceCull
        {
            get => _backfaceCull;
            set { if (SetField(ref _backfaceCull, value)) { Scene.BackfaceCull = value; UpdateRenderModeStatus(); } }
        }

        private bool _drawGrid;
        public bool DrawGrid
        {
            get => _drawGrid;
            set
            {
                if (SetField(ref _drawGrid, value))
                {
                    Scene.DrawViewGrid = value;
                }
            }
        }

        private bool _drawCollisionMeshes;
        public bool DrawCollisionMeshes
        {
            get => _drawCollisionMeshes;
            set
            {
                if (SetField(ref _drawCollisionMeshes, value))
                {
                    Scene.DrawCollisionMeshes = value;
                }
            }
        }

        private bool _drawBoundingBoxes;
        public bool DrawBoundingBoxes
        {
            get => _drawBoundingBoxes;
            set
            {
                if (SetField(ref _drawBoundingBoxes, value))
                {
                    Scene.DrawBoundingBoxes = value;
                }
            }
        }

        private bool _drawDummies;
        public bool DrawDummies
        {
            get => _drawDummies;
            set
            {
                if (SetField(ref _drawDummies, value))
                {
                    Scene.DrawDummyHelpers = value;
                }
            }
        }

        private bool _drawLightHelpers;
        public bool DrawLightHelpers
        {
            get => _drawLightHelpers;
            set
            {
                if (SetField(ref _drawLightHelpers, value))
                {
                    Scene.DrawLightHelpers = value;
                }
            }
        }

        private bool _drawAxisViewport;
        public bool DrawAxisViewport
        {
            get => _drawAxisViewport;
            set
            {
                if (SetField(ref _drawAxisViewport, value))
                {
                    Scene.DrawViewAxis = value;
                }
            }
        }

        private bool _drawWorldCenter;
        public bool DrawWorldCenter
        {
            get => _drawWorldCenter;
            set
            {
                if (SetField(ref _drawWorldCenter, value))
                {
                    Scene.DrawWorldCenter = value;
                }
            }
        }

        private bool _defaultToAxisMovement;
        public bool DefaultToAxisMovement
        {
            get => _defaultToAxisMovement;
            set
            {
                if (SetField(ref _defaultToAxisMovement, value))
                {
                    Scene.DefaultToAxisMovement = value;
                }
            }
        }

        #endregion
    }
}
