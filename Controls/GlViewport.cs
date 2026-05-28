using System;
using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using OpenTK.Graphics.OpenGL;

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
                Logger.LogInfo(string.Format("OpenGL {0} | GLSL {1} | {2}\n",
                    GL.GetString(StringName.Version),
                    GL.GetString(StringName.ShadingLanguageVersion),
                    GL.GetString(StringName.Renderer)));
            }
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
                catch (Exception ex) { Logger.LogError(string.Format("GL job failed: {0}\n", ex)); }
            }

            Scene.Render();

            // Continuous redraw loop (replaces the old WinForms render timer).
            RequestNextFrameRendering();
        }
    }
}
