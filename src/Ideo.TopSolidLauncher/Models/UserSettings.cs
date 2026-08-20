namespace Ideo.TopSolidLauncher.Models;

public sealed class UserSettings
{
    public string CatalogPath { get; set; } = string.Empty;
    public bool CompactMode { get; set; }
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 820;
    public List<Guid> FavoriteCardIds { get; set; } = [];
    public List<Guid> RecentCardIds { get; set; } = [];
    public List<SavedView> SavedViews { get; set; } = [];
}

public sealed class SavedView
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Ma vue";
    public string SearchText { get; set; } = string.Empty;
    public List<Guid> SelectedTagIds { get; set; } = [];
}
