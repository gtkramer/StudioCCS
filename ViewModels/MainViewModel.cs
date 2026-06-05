using System.Collections.ObjectModel;
using StudioCCS.Rendering;

namespace StudioCCS.ViewModels;

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
    public ObservableCollection<CCSTreeNode> CCSRoots { get; } = new ObservableCollection<CCSTreeNode>();
    public CCSTreeNode AnimationsRoot { get; } = new CCSTreeNode("Animations");
    public ObservableCollection<CCSTreeNode> SceneRoots { get; }

    public MainViewModel()
    {
        SceneRoots = new ObservableCollection<CCSTreeNode> { AnimationsRoot };

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

    // Live camera readout, split per value so the status bar can place each one in
    // its own fixed-width box - that keeps the columns from jittering as the values
    // change, without needing a monospace font. Rotations carry the degree suffix.
    private string _camRotX = "";
    public string CamRotX
    {
        get => _camRotX;
        private set => SetField(ref _camRotX, value);
    }

    private string _camRotY = "";
    public string CamRotY
    {
        get => _camRotY;
        private set => SetField(ref _camRotY, value);
    }

    private string _camRotZ = "";
    public string CamRotZ
    {
        get => _camRotZ;
        private set => SetField(ref _camRotZ, value);
    }

    private string _camTargetX = "";
    public string CamTargetX
    {
        get => _camTargetX;
        private set => SetField(ref _camTargetX, value);
    }

    private string _camTargetY = "";
    public string CamTargetY
    {
        get => _camTargetY;
        private set => SetField(ref _camTargetY, value);
    }

    private string _camTargetZ = "";
    public string CamTargetZ
    {
        get => _camTargetZ;
        private set => SetField(ref _camTargetZ, value);
    }

    private string _camDistance = "";
    public string CamDistance
    {
        get => _camDistance;
        private set => SetField(ref _camDistance, value);
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
        CamRotX = string.Format("{0:0.#}°", cam.Rotation.X);
        CamRotY = string.Format("{0:0.#}°", cam.Rotation.Y);
        CamRotZ = string.Format("{0:0.#}°", cam.Rotation.Z);
        CamTargetX = string.Format("{0:0.#}", cam.Target.X);
        CamTargetY = string.Format("{0:0.#}", cam.Target.Y);
        CamTargetZ = string.Format("{0:0.#}", cam.Target.Z);
        CamDistance = string.Format("{0:0.#}", cam.Distance);
    }

    private void UpdateRenderModeStatus()
    {
        List<string> options = new List<string>();
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

    #region Loading progress

    // Backs the progress bar that only appears while a file-load batch is in
    // flight. Counts are cumulative across overlapping batches: a second Load (or
    // a drag-drop) started while the first is still running just extends the same
    // bar rather than resetting it. When every file in flight has finished the bar
    // hides and the counts reset to zero. All mutation happens on the UI thread
    // (callers marshal there), so no locking is needed.
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    private int _loadedCount;
    public int LoadedCount
    {
        get => _loadedCount;
        private set => SetField(ref _loadedCount, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        private set => SetField(ref _totalCount, value);
    }

    private string _loadProgressText = "";
    public string LoadProgressText
    {
        get => _loadProgressText;
        private set => SetField(ref _loadProgressText, value);
    }

    /// <summary>Registers a batch of <paramref name="count"/> files about to load, showing the bar.</summary>
    public void BeginLoading(int count)
    {
        TotalCount += count;
        IsLoading = TotalCount > 0;
        UpdateLoadProgressText();
    }

    /// <summary>
    /// Marks one file as fully loaded. Called exactly once per file at its terminal
    /// point (GL upload done, or parse failed/skipped). Hides the bar and resets
    /// the counts once the whole in-flight batch is accounted for.
    /// </summary>
    public void ReportFileLoaded()
    {
        LoadedCount++;
        UpdateLoadProgressText();
        if (LoadedCount >= TotalCount)
        {
            IsLoading = false;
            LoadedCount = 0;
            TotalCount = 0;
        }
    }

    private void UpdateLoadProgressText()
    {
        LoadProgressText = string.Format("Loading {0} of {1}", LoadedCount, TotalCount);
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
