using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BugzillaDumper.Services;

public static class PlatformHelpers
{
    public static void OpenFolder(string folderPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{folderPath}\"", UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start(new ProcessStartInfo { FileName = "open", Arguments = $"\"{folderPath}\"", UseShellExecute = false });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start(new ProcessStartInfo { FileName = "xdg-open", Arguments = $"\"{folderPath}\"", UseShellExecute = false });
    }
}
