namespace Ideo.TopSolidLauncher.Models;

public sealed class LauncherTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Autre";
    public string Color { get; set; } = "#315D9D";
    public int SortOrder { get; set; }
}
