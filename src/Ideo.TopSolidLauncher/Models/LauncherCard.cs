namespace Ideo.TopSolidLauncher.Models;

public sealed class LauncherCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Nouvelle application";
    public string Subtitle { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string TargetPath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#2E69B3";
    public List<LauncherQuickAccess> QuickAccessLinks { get; set; } = [];
    public List<Guid> TagIds { get; set; } = [];
    public int SortOrder { get; set; }
    public bool RunAsAdministrator { get; set; }
    public bool MinimizeAfterLaunch { get; set; } = true;

    public LauncherCard Clone()
    {
        return new LauncherCard
        {
            Id = Id,
            Title = Title,
            Subtitle = Subtitle,
            GroupId = GroupId,
            TargetPath = TargetPath,
            Arguments = Arguments,
            WorkingDirectory = WorkingDirectory,
            LogoPath = LogoPath,
            AccentColor = AccentColor,
            QuickAccessLinks = QuickAccessLinks.Select(link => link.Clone()).ToList(),
            TagIds = [.. TagIds],
            SortOrder = SortOrder,
            RunAsAdministrator = RunAsAdministrator,
            MinimizeAfterLaunch = MinimizeAfterLaunch
        };
    }
}
