using Avalonia.Controls;
namespace StudioCCS.Views
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();
        }

        public void SetReportText(string text)
        {
            reportText.Text = text;
        }
    }
}
