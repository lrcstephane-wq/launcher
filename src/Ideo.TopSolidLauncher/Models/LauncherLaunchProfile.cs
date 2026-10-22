namespace Ideo.TopSolidLauncher.Models;

public sealed class LauncherLaunchProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Par défaut";
    public string TargetPath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public List<LauncherQuickAccess> QuickAccessLinks { get; set; } = [];
    public int SortOrder { get; set; }
    public bool RunAsAdministrator { get; set; }
    public bool MinimizeAfterLaunch { get; set; } = true;

    public LauncherLaunchProfile Clone() => new()
    {
        Id = Id,
        Name = Name,
        TargetPath = TargetPath,
        Arguments = Arguments,
        WorkingDirectory = WorkingDirectory,
        QuickAccessLinks = QuickAccessLinks.Select(link => link.Clone()).ToList(),
        SortOrder = SortOrder,
        RunAsAdministrator = RunAsAdministrator,
        MinimizeAfterLaunch = MinimizeAfterLaunch
    };
}
