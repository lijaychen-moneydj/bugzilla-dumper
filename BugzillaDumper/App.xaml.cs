using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using BugzillaDumper.Services;
using BugzillaDumper.ViewModels;

namespace BugzillaDumper;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BugzillaDumper", "crash.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        // Hook all unhandled exception sources
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);

        try
        {
            var httpClient = new HttpClient();
            var bugzillaService = new BugzillaService(httpClient);
            var viewModel = new MainViewModel(bugzillaService);

            var window = new MainWindow(viewModel);
            window.Show();
        }
        catch (Exception ex)
        {
            LogAndShow(ex, "Startup");
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogAndShow(e.Exception, "DispatcherUnhandledException");
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogAndShow(ex, "AppDomainUnhandledException");
    }

    private static void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        LogAndShow(e.Exception, "UnobservedTaskException");
        e.SetObserved();
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

        MessageBox.Show(
            $"Error: {ex.Message}\n\nType: {ex.GetType().FullName}\n\nLog: {LogPath}",
            "Bugzilla Dumper - Unhandled Exception",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
