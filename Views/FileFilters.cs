using Avalonia.Platform.Storage;

namespace StudioCCS.Views
{
    /// <summary>Shared file-picker type descriptors, reused by the load/export dialogs.</summary>
    internal static class FileFilters
    {
        public static readonly FilePickerFileType All = new FilePickerFileType("All Files") { Patterns = new[] { "*" } };
        public static readonly FilePickerFileType CCS = new FilePickerFileType("CCS Files") { Patterns = new[] { "*.ccs", "*.tmp" } };
        public static readonly FilePickerFileType Bin = new FilePickerFileType("Bin Files") { Patterns = new[] { "*.bin" } };
    }
}
