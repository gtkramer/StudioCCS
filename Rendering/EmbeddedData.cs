using Avalonia.Platform;

namespace StudioCCS.Rendering;

/// <summary>
/// Opens the data files (shaders, vertex blobs) that are embedded in the
/// assembly as AvaloniaResource items. The relative path passed in matches
/// the project-relative path of the file (e.g. "Rendering/Shaders/Grid.vsh"),
/// which AvaloniaResource maps to "avares://StudioCCS/&lt;path&gt;".
/// </summary>
public static class EmbeddedData
{
    private const string BaseUri = "avares://StudioCCS/";

    public static Stream Open(string relativePath)
    {
        return AssetLoader.Open(new Uri(BaseUri + relativePath));
    }

    public static string ReadAllText(string relativePath)
    {
        using (StreamReader sr = new StreamReader(Open(relativePath)))
        {
            return sr.ReadToEnd();
        }
    }
}
