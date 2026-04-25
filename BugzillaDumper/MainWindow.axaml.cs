using Avalonia.Controls;

namespace BugzillaDumper;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = AppVersion.Full;
        TitleVersionText.Text  = $"v{AppVersion.Version}";
        StatusVersionText.Text = AppVersion.Full;
    }
}
