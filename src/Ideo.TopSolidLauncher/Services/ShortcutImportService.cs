using System.IO;

namespace Ideo.TopSolidLauncher.Services;

public static class ShortcutImportService
{
    public static ShortcutData Read(string shortcutPath)
    {
        if (!File.Exists(shortcutPath) || !shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Sélectionnez un raccourci Windows .lnk valide.");

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Le composant Windows de lecture des raccourcis est indisponible.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);

        var iconLocation = (string?)shortcut.IconLocation ?? string.Empty;
        var iconPath = iconLocation.Split(',')[0].Trim().Trim('"');
        return new ShortcutData(
            Path.GetFileNameWithoutExtension(shortcutPath),
            (string?)shortcut.TargetPath ?? string.Empty,
            (string?)shortcut.Arguments ?? string.Empty,
            (string?)shortcut.WorkingDirectory ?? string.Empty,
            File.Exists(iconPath) ? iconPath : string.Empty);
    }
}

public sealed record ShortcutData(
    string Title,
    string TargetPath,
    string Arguments,
    string WorkingDirectory,
    string IconPath);
