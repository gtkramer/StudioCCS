using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace StudioCCS.Views
{
    public partial class ExportToObjWindow : Window
    {
        private TextBox _txtExportPath;
        private CheckBox _chkModelWithNormals;
        private CheckBox _chkSplitSubModels;
        private CheckBox _chkExportCollision;
        private CheckBox _chkSplitCollision;
        private CheckBox _chkExportDummies;
        private CheckBox _chkDumpAnime;
        private Button _btnDoExport;

        public ExportToObjWindow()
        {
            InitializeComponent();

            _txtExportPath = this.FindControl<TextBox>("txtExportPath");
            _chkModelWithNormals = this.FindControl<CheckBox>("chkModelWithNormals");
            _chkSplitSubModels = this.FindControl<CheckBox>("chkSplitSubModels");
            _chkExportCollision = this.FindControl<CheckBox>("chkExportCollision");
            _chkSplitCollision = this.FindControl<CheckBox>("chkSplitCollision");
            _chkExportDummies = this.FindControl<CheckBox>("chkExportDummies");
            _chkDumpAnime = this.FindControl<CheckBox>("chkDumpAnime");
            _btnDoExport = this.FindControl<Button>("btnDoExport");

            _txtExportPath.TextChanged += (_, _) =>
            {
                _btnDoExport.IsEnabled = !string.IsNullOrEmpty(_txtExportPath.Text);
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // ----- Result, read by MainWindow after the dialog returns true -----
        public string ExportPath => _txtExportPath.Text ?? "";
        public bool WithNormals => _chkModelWithNormals.IsChecked == true;
        public bool SplitSubModels => _chkSplitSubModels.IsChecked == true;
        public bool ExportCollision => _chkExportCollision.IsChecked == true;
        public bool SplitCollision => _chkSplitCollision.IsChecked == true;
        public bool ExportDummies => _chkExportDummies.IsChecked == true;
        public bool DumpAnime => _chkDumpAnime.IsChecked == true;

        /// <summary>Restrict options to those that make sense for SMD export.</summary>
        public void ConfigureForSmd()
        {
            Title = "Export to SMD...";
            _chkSplitCollision.IsEnabled = false;
            _chkSplitSubModels.IsEnabled = false;
            _chkExportCollision.IsEnabled = false;
            _chkExportDummies.IsEnabled = false;
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
                _txtExportPath.Text = folder.Path.LocalPath;
            }
        }

        private void OnExportCollisionChanged(object sender, RoutedEventArgs e)
        {
            _chkSplitCollision.IsEnabled = _chkExportCollision.IsChecked == true;
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
