using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StudioCCS.Views
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetReportText(string reportText)
        {
            this.FindControl<TextBox>("reportText").Text = reportText;
        }
    }
}
