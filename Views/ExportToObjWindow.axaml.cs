using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
namespace StudioCCS.Views
{
    public partial class ExportToObjWindow : Window
    {
        public ExportToObjWindow()
        {
            InitializeComponent();

            txtExportPath.TextChanged += (_, _) =>
            {
                btnDoExport.IsEnabled = !string.IsNullOrEmpty(txtExportPath.Text);
            };
        }

        // ----- Result, read by MainWindow after the dialog returns true -----
        public string ExportPath => txtExportPath.Text ?? "";
        public bool WithNormals => chkModelWithNormals.IsChecked == true;
        public bool SplitSubModels => chkSplitSubModels.IsChecked == true;
        public bool ExportCollision => chkExportCollision.IsChecked == true;
        public bool SplitCollision => chkSplitCollision.IsChecked == true;
        public bool ExportDummies => chkExportDummies.IsChecked == true;
        public bool DumpAnime => chkDumpAnime.IsChecked == true;

        /// <summary>Restrict options to those that make sense for SMD export.</summary>
        public void ConfigureForSmd()
        {
            Title = "Export to SMD...";
            chkSplitCollision.IsEnabled = false;
            chkSplitSubModels.IsEnabled = false;
            chkExportCollision.IsEnabled = false;
            chkExportDummies.IsEnabled = false;
        }

        private async void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select directory to export to...",
                AllowMultiple = false,
            });

            var folder = folders.FirstOrDefault();
            if (folder != null)
            {
                txtExportPath.Text = folder.Path.LocalPath;
            }
        }

        private void OnExportCollisionChanged(object sender, RoutedEventArgs e)
        {
            chkSplitCollision.IsEnabled = chkExportCollision.IsChecked == true;
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
