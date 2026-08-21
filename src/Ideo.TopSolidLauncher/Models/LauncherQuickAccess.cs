namespace Ideo.TopSolidLauncher.Models;

public sealed class LauncherQuickAccess
{
    public string Label { get; set; } = "Dossier";
    public string Path { get; set; } = string.Empty;

    public LauncherQuickAccess Clone() => new()
    {
        Label = Label,
        Path = Path
    };
}
