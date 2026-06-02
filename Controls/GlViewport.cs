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
namespace StudioCCS.Controls
{
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
            public AvaloniaBindingsContext(GlInterface gl) => _gl = gl;
            public IntPtr GetProcAddress(string procName) => _gl.GetProcAddress(procName);
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

        /// <summary>
        /// Queues an action to run inside the next render callback with the GL
        /// context current. Safe to call from any thread (e.g. a background parse
        /// task); the render wake-up is marshalled to the UI thread.
        /// </summary>
        public void EnqueueGlJob(Action job)
        {
            _glJobs.Enqueue(job);
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

            // Run queued GL work (file load/unload) now that the context is current.
            while (_glJobs.TryDequeue(out var job))
            {
                try { job(); }
                catch (Exception ex) { Log.Error(string.Format("GL job failed: {0}\n", ex)); }
            }

            Scene.Render();

            // Continuous redraw loop (replaces the old WinForms render timer).
            RequestNextFrameRendering();
        }
    }
}
