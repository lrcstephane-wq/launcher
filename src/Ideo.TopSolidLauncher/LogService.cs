using System.Diagnostics;
using System.IO;

namespace Ideo.TopSolidLauncher;

public static class LogService
{
    private static readonly object SyncRoot = new();

    public static string LogFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ideo",
        "TopSolidLauncher");

    public static string LogPath => Path.Combine(LogFolder, "launcher.log");

    public static void Write(string message, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}";
            if (exception is not null)
                line += $"{Environment.NewLine}{exception}";

            lock (SyncRoot)
                File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
            // Logging must never prevent the launcher from working.
        }
    }

    public static void OpenLogFolder()
    {
        Directory.CreateDirectory(LogFolder);
        if (!File.Exists(LogPath))
            Write("Journal initialisé.");

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{LogPath}\"",
            UseShellExecute = true
        });
    }
}
