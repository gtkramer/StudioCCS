using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using OpenTK.Graphics.OpenGL;
using StudioCCS.Logging;
using StudioCCS.Rendering;

namespace StudioCCS.Controls;

/// <summary>
/// Hosts the OpenTK-based <see cref="Scene"/> renderer inside Avalonia.
/// Avalonia owns the GL context (creation, MakeCurrent and buffer swap); we
/// load OpenTK's GL bindings against that context and let Scene issue its
/// existing GL.* calls into Avalonia's framebuffer each frame.
/// </summary>
public class GlViewport : OpenGlControlBase
{
    private sealed class AvaloniaBindingsContext : OpenTK.IBindingsContext
    {
        private readonly GlInterface _gl;
        public AvaloniaBindingsContext(GlInterface gl)
        {
            _gl = gl;
        }

        public IntPtr GetProcAddress(string procName)
        {
            return _gl.GetProcAddress(procName);
        }
    }

    private bool _bindingsLoaded;
    private bool _sceneInit;

    // Cleared if the negotiated context is older than our 3.2 minimum; the
    // render callback then skips Scene work instead of binding shaders that
    // never compiled and spamming GL errors on a black viewport every frame.
    private bool _contextSupported = true;

    // The debug callback is handed to the driver as a function pointer, so a
    // managed reference must outlive the call to keep it from being collected.
    private static DebugProc _debugProc;

    // Work that must run with the GL context current. The context is only
    // current *inside* the OnOpenGlInit/OnOpenGlRender callbacks (which Avalonia
    // invokes on the UI thread in this configuration). Any GL resource work
    // triggered from a normal UI handler (loading/unloading a CCS file builds
    // shaders, textures, VBOs) therefore runs with NO current context unless it
    // is funneled through here to be executed during the next render callback.
    // ConcurrentQueue is used defensively in case Avalonia ever renders the
    // control on a dedicated render thread.
    private readonly ConcurrentQueue<Action> _glJobs = new ConcurrentQueue<Action>();

    // Upper bound on time spent running queued GL jobs per render frame. Draining
    // the whole queue in one frame blocks the UI thread (this control renders on
    // it) for the full duration of the uploads, which on a large load batch stalls
    // it for seconds. Capping the per-frame spend spreads the batch across frames
    // - the continuous redraw loop picks up the remainder next frame - so the UI
    // and the load progress bar stay responsive and animate smoothly. ~8ms leaves
    // the rest of a 60fps (16.7ms) frame for Scene.Render.
    private const double GlJobBudgetMs = 8.0;

    public GlViewport()
    {
        // Render on demand (see Scene's render-on-demand region): wake the loop
        // when our visible area changes (resize/layout) and whenever any code
        // requests a redraw via Scene.RequestRedraw. The Scene subscription is
        // tied to visual-tree lifetime so the static event never outlives the
        // control.
        EffectiveViewportChanged += (_, _) => WakeRenderLoop();
        AttachedToVisualTree += (_, _) => Scene.RedrawRequested += WakeRenderLoop;
        DetachedFromVisualTree += (_, _) => Scene.RedrawRequested -= WakeRenderLoop;
    }

    /// <summary>
    /// Queues an action to run inside the next render callback with the GL
    /// context current. Safe to call from any thread (e.g. a background parse
    /// task); the render wake-up is marshalled to the UI thread.
    /// </summary>
    public void EnqueueGlJob(Action job)
    {
        _glJobs.Enqueue(job);
        WakeRenderLoop();
    }

    // Schedules a single render frame, marshalling onto the UI thread when called
    // from elsewhere. This is the one place that pokes Avalonia's render callback
    // awake: it backs EnqueueGlJob, the Scene.RedrawRequested subscription, and
    // the viewport-size hook, so every redraw request funnels through here.
    private void WakeRenderLoop()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            RequestNextFrameRendering();
        }
        else
        {
            Dispatcher.UIThread.Post(RequestNextFrameRendering);
        }
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        if (!_bindingsLoaded)
        {
            GL.LoadBindings(new AvaloniaBindingsContext(gl));
            _bindingsLoaded = true;
            Log.Info(string.Format("OpenGL {0} | GLSL {1} | {2}\n",
                GL.GetString(StringName.Version),
                GL.GetString(StringName.ShadingLanguageVersion),
                GL.GetString(StringName.Renderer)));

            // The CCS shaders are #version 330, so the context must be desktop
            // GL 3.3+ (core profile, geometry shaders, texture buffers). 3.3 is
            // the floor on every platform; macOS satisfies it with a 4.1 context
            // (it offers only 3.2 or 4.1 core, never 3.3). If a platform negotiates
            // something older, fail loudly here rather than letting every shader
            // fail to compile and render nothing with no explanation.
            GL.GetInteger(GetPName.MajorVersion, out int major);
            GL.GetInteger(GetPName.MinorVersion, out int minor);
            if (major < 3 || (major == 3 && minor < 3))
            {
                _contextSupported = false;
                Log.Error(string.Format(
                    "Unsupported OpenGL context: got {0}.{1}, but StudioCCS needs 3.3 or " +
                    "newer (core profile with geometry shaders). The 3D viewport is disabled.\n",
                    major, minor));
                return;
            }

            EnableDebugOutput(major, minor);
        }
    }

    // Routes the driver's debug messages into our log in debug builds. KHR_debug
    // is core in GL 4.3; on older contexts (e.g. macOS' 4.1) it is unavailable
    // and we simply skip it - the capability guard above still catches hard
    // failures. No-op entirely in release builds.
    [Conditional("DEBUG")]
    private void EnableDebugOutput(int major, int minor)
    {
        if (major < 4 || (major == 4 && minor < 3))
        {
            return;
        }

        _debugProc = DebugCallback;
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
    }

    private static void DebugCallback(DebugSource source, DebugType type, int id,
        DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
    {
        if (severity == DebugSeverity.DebugSeverityNotification)
        {
            return;
        }

        string text = Marshal.PtrToStringUTF8(message, length);
        Log.Warning(string.Format("GL [{0}/{1}]: {2}\n", severity, type, text));
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        if (_sceneInit)
        {
            Scene.DeInit();
            _sceneInit = false;
        }
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (!_contextSupported)
        {
            // Distinct fill signals the disabled viewport; the explanation is
            // in the log. Return without requesting another frame so we do not
            // spin on a context we cannot draw into.
            GL.ClearColor(0.15f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            return;
        }

        if (Scene.ExportInProgress)
        {
            // A long operation (a scene export) is touching scene state off the
            // UI thread; skip all scene work this frame so we don't race it, and
            // don't re-arm - it requests a redraw when it finishes. Clear to the
            // scene background (a plain colour field, not the geometry the export
            // reads) so the viewport behind the modal stays consistent.
            GL.ClearColor(Scene.BackgroundColor.X, Scene.BackgroundColor.Y, Scene.BackgroundColor.Z, Scene.BackgroundColor.W);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
            return;
        }

        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        int pw = Math.Max(1, (int)(Bounds.Width * scaling));
        int ph = Math.Max(1, (int)(Bounds.Height * scaling));
        Scene.ViewWidth = pw;
        Scene.ViewHeight = ph;

        if (!_sceneInit)
        {
            Scene.Init();
            _sceneInit = true;
        }

        // Run queued GL work (file load/unload) now that the context is current,
        // but only up to GlJobBudgetMs this frame so a big load batch can't block
        // the UI thread for seconds at a stretch. Whatever doesn't fit is left on
        // the queue and drained on later frames (RequestNextFrameRendering below
        // keeps the loop alive). The time check is after each job, so at least one
        // always runs per frame - forward progress is guaranteed even if a single
        // upload overruns the budget on its own.
        long drainStart = Stopwatch.GetTimestamp();
        double budgetTicks = GlJobBudgetMs * Stopwatch.Frequency / 1000.0;
        while (_glJobs.TryDequeue(out var job))
        {
            try { job(); }
            catch (Exception ex) { Log.Error(string.Format("GL job failed: {0}\n", ex)); }
            if (Stopwatch.GetTimestamp() - drainStart >= budgetTicks)
            {
                break;
            }
        }

        Scene.Render();

        // Render on demand: re-arm only while there's a reason to keep drawing -
        // GL jobs still queued (a load batch draining across frames) or the scene
        // is live (a held camera key or a playing animation). Otherwise the loop
        // idles here until the next RequestRedraw wakes it, so a static view at
        // rest costs nothing. Replaces the old unconditional re-arm that pinned
        // the loop at the display refresh rate forever.
        if (!_glJobs.IsEmpty || Scene.WantsContinuousRender())
        {
            RequestNextFrameRendering();
        }
    }
}
