using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BugzillaDumper.Services;

public class UpdateService
{
    private const string DeployPath = @"\\j-nas01\部門_研發二部\QA\tools\BugzillaDumper";
    private const string RemoteVersionFile = DeployPath + @"\version.txt";
    private const string RemoteExe = DeployPath + @"\BugzillaDumper.exe";

    /// <summary>
    /// Checks the remote version.txt. Returns the remote version string if it is
    /// newer than the running version, otherwise null.
    /// </summary>
    public async Task<string?> CheckAsync()
    {
        try
        {
            var remoteRaw = await Task.Run(() => File.ReadAllText(RemoteVersionFile)).ConfigureAwait(false);
            var remoteStr = remoteRaw.Trim();

            if (TryParseVersion(remoteStr, out var remote) &&
                TryParseVersion(AppVersion.Version, out var current) &&
                remote > current)
                return remoteStr;
        }
        catch
        {
            // Network unreachable, share offline, no permission — silently ignore
        }
        return null;
    }

    /// <summary>
    /// Copies the remote exe to %TEMP%, writes a bat that waits for this process to
    /// exit then swaps the file and restarts, then shuts down the app.
    /// </summary>
    public void ApplyUpdate(Action<string> onStatus)
    {
        var currentExe = Process.GetCurrentProcess().MainModule?.FileName
                         ?? Environment.ProcessPath
                         ?? throw new InvalidOperationException("Cannot determine current exe path.");

        var tempExe = Path.Combine(Path.GetTempPath(), "BugzillaDumper_new.exe");
        var batPath = Path.Combine(Path.GetTempPath(), "bugzilla_update.bat");

        onStatus("正在下載新版本…");
        File.Copy(RemoteExe, tempExe, overwrite: true);

        onStatus("準備重啟…");
        var bat = $"""
            @echo off
            timeout /t 2 /nobreak >nul
            copy /y "{tempExe}" "{currentExe}"
            start "" "{currentExe}"
            del "{tempExe}"
            del "%~f0"
            """;
        File.WriteAllText(batPath, bat);

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batPath}\"",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false
        });

        System.Windows.Application.Current.Dispatcher.Invoke(
            System.Windows.Application.Current.Shutdown);
    }

    private static bool TryParseVersion(string s, out Version version)
    {
        // Pad short versions like "0.16" → "0.16.0.0" for Version.TryParse
        var parts = s.Split('.');
        while (parts.Length < 2) s += ".0";
        return Version.TryParse(s, out version!);
    }
}
