using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.OpenGL;

namespace StudioCCS
{
    internal sealed class Program
    {
        // The CCS shaders are desktop GLSL #version 330 and use geometry shaders,
        // so every platform must hand us a desktop OpenGL 3.3 (or newer) core
        // context rather than the GLES/ANGLE default. Listed newest-first; the
        // backend negotiates the first one the driver can provide.
        private static List<GlVersion> DesktopGlProfiles() => new List<GlVersion>
        {
            new GlVersion(GlProfileType.OpenGL, 4, 0),
            new GlVersion(GlProfileType.OpenGL, 3, 3),
            new GlVersion(GlProfileType.OpenGL, 3, 2),
        };

        [STAThread]
        public static void Main(string[] args) =>
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect();

            if (OperatingSystem.IsWindows())
            {
                // Windows defaults to ANGLE, which only exposes OpenGL ES and so
                // cannot compile our desktop #version 330 / geometry shaders.
                // Switch the backend to native WGL and request a desktop profile.
                // (This routes Avalonia's own compositor through WGL too; that is
                // the supported way to get a desktop GL context here.)
                builder = builder.With(new Win32PlatformOptions
                {
                    RenderingMode = new[] { Win32RenderingMode.Wgl },
                    WglProfiles = DesktopGlProfiles(),
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                builder = builder.With(new X11PlatformOptions
                {
                    GlProfiles = DesktopGlProfiles(),
                });
            }
            // macOS (AvaloniaNative) exposes no GL-version selector: the OS hands
            // back an OpenGL 3.2 or 4.1 Core context of its choosing. Geometry
            // shaders work on both, but #version 330 compiles only on the 4.1 Core
            // context (3.2 Core caps GLSL at 1.50). This must be verified on real
            // Mac hardware; if it lands on a 3.2 context the shaders need a
            // #version 150 variant. Apple has deprecated OpenGL, so macOS is the
            // least certain target here.

            return builder
                .WithInterFont()
                .LogToTrace();
        }
    }
}
