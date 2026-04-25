using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BugzillaDumper.Services;
using BugzillaDumper.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace BugzillaDumper;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BugzillaDumper", "crash.log");

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var httpClient = new HttpClient();

                // GitLab 常用自架部署且憑證未必有效，這裡略過 server cert 檢查
                var gitLabHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                var gitLabHttpClient = new HttpClient(gitLabHandler);
                var bugzillaService  = new BugzillaService(httpClient);
                var updateService    = new UpdateService();
                var gitLabService    = new GitLabService(gitLabHttpClient);

                var window = new MainWindow();
                var picker = new AvaloniaFilePickerService(window);
                var viewModel = new MainViewModel(bugzillaService, updateService, gitLabService, picker);
                window.DataContext = viewModel;

                desktop.MainWindow = window;

                _ = CheckForUpdatesAsync(viewModel, updateService);
                StartUpdateTimer(viewModel, updateService);
            }
            catch (Exception ex)
            {
                LogAndShow(ex, "Startup");
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogAndShow(ex, "AppDomainUnhandledException");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogAndShow(e.Exception, "UnobservedTaskException");
        e.SetObserved();
    }

    private static void StartUpdateTimer(MainViewModel viewModel, UpdateService updateService)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        timer.Tick += (_, _) => _ = CheckForUpdatesAsync(viewModel, updateService);
        timer.Start();
    }

    private static async Task CheckForUpdatesAsync(MainViewModel viewModel, UpdateService updateService)
    {
        if (viewModel.UpdateAvailable) return;

        var newVersion = await updateService.CheckAsync();
        if (newVersion is not null)
            await Dispatcher.UIThread.InvokeAsync(() => viewModel.SetUpdateAvailable(newVersion));
    }

    private static void LogAndShow(Exception ex, string source)
    {
        var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n{ex}\n\n";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, message);
        }
        catch { /* log write failed — still show the dialog */ }

        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Bugzilla Dumper - Unhandled Exception",
                $"Error: {ex.Message}\n\nType: {ex.GetType().FullName}\n\nLog: {LogPath}",
                ButtonEnum.Ok,
                Icon.Error);
            await box.ShowAsync();
        });
    }
}
