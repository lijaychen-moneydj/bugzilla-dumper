using System.Windows;
using BugzillaDumper.ViewModels;

namespace BugzillaDumper;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Title = AppVersion.Full;
        TitleVersionText.Text = $"v{AppVersion.Version}";
        StatusVersionText.Text = AppVersion.Full;
    }
}
