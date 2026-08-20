namespace Ideo.TopSolidLauncher.Models;

public sealed class LauncherGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Nouveau groupe";
    public int SortOrder { get; set; }
}
