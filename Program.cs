using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.OpenGL;

namespace StudioCCS
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args) =>
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions
                {
                    // The CCS shaders are desktop GLSL #version 330 (and use geometry
                    // shaders), so we must negotiate a desktop OpenGL 3.3 context rather
                    // than the GLES default.
                    GlProfiles = new List<GlVersion>
                    {
                        new GlVersion(GlProfileType.OpenGL, 3, 3),
                        new GlVersion(GlProfileType.OpenGL, 3, 2),
                        new GlVersion(GlProfileType.OpenGL, 4, 0),
                    },
                })
                .WithInterFont()
                .LogToTrace();
    }
}
