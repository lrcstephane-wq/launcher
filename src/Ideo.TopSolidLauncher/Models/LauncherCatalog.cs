namespace Ideo.TopSolidLauncher.Models;

public sealed class LauncherCatalog
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<LauncherGroup> Groups { get; set; } = [];
    public List<LauncherTag> Tags { get; set; } = [];
    public List<LauncherCard> Cards { get; set; } = [];
}
