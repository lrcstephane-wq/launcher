using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public static class DefaultCatalogFactory
{
    public static LauncherCatalog Create()
    {
        var group = new LauncherGroup { Name = "Environnements Idéo", SortOrder = 0 };
        var workTag = NewTag("Travail", "Usage", "#2E69B3", 0);
        var topSolidTag = NewTag("TopSolid'Wood", "Application", "#52667D", 0);
        var yearTag = NewTag("2021", "Année", "#6B5BA7", 0);
        var tags = new List<LauncherTag> { workTag, topSolidTag, yearTag };
        var cards = new List<LauncherCard>();

        foreach (var installation in TopSolidDiscovery.FindInstallations())
        {
            var versionTag = NewTag(installation.Version, "Version", "#C98247", tags.Count);
            tags.Add(versionTag);
            cards.Add(new LauncherCard
            {
                Title = $"TopSolid'Wood {installation.Version}",
                Subtitle = "Base de travail",
                GroupId = group.Id,
                TargetPath = installation.ExecutablePath,
                Arguments = installation.Arguments,
                WorkingDirectory = installation.FolderPath,
                AccentColor = "#2E69B3",
                TagIds = [workTag.Id, topSolidTag.Id, yearTag.Id, versionTag.Id],
                SortOrder = cards.Count,
                MinimizeAfterLaunch = true
            });
        }

        return new LauncherCatalog
        {
            Groups = [group],
            Tags = tags,
            Cards = cards
        };
    }

    private static LauncherTag NewTag(string name, string category, string color, int sortOrder) => new()
    {
        Name = name,
        Category = category,
        Color = color,
        SortOrder = sortOrder
    };
}
