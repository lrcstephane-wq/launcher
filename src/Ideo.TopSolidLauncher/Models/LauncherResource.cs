namespace Ideo.TopSolidLauncher.Models;

public sealed class LauncherResource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Nouvelle ressource";
    public string IconGlyph { get; set; } = "\uE8B7";
    public int SortOrder { get; set; }
    public Guid DefaultLocationId { get; set; }
    public List<LauncherResourceLocation> Locations { get; set; } = [];

    public LauncherResource Clone() => new()
    {
        Id = Id,
        Name = Name,
        IconGlyph = IconGlyph,
        SortOrder = SortOrder,
        DefaultLocationId = DefaultLocationId,
        Locations = Locations.Select(location => location.Clone()).ToList()
    };
}

public sealed class LauncherResourceLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Local";
    public string Target { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public LauncherResourceLocation Clone() => new()
    {
        Id = Id,
        Name = Name,
        Target = Target,
        SortOrder = SortOrder
    };
}
