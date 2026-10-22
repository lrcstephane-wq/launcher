using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public static class DefaultCatalogFactory
{
    public static readonly Guid TopSolidDownloadsResourceId = Guid.Parse("c28e1b7a-e7db-4b86-9c38-07e6bfa1d6ad");
    public static readonly Guid TopSolidDownloadsLocationId = Guid.Parse("4594cb2f-8124-4018-a857-a6898ba70546");
    public static readonly Guid TopSolidLicensesResourceId = Guid.Parse("5b678d31-8d99-4057-99a8-34ee41c271a8");
    public static readonly Guid TopSolidLicensesLocalId = Guid.Parse("ec5e01ec-054e-4fa8-a008-d2b99a6751c7");
    public static readonly Guid TopSolidLicensesRemoteId = Guid.Parse("f977c451-2f28-4d68-93f9-c0dc0d1a2f03");
    public const string TopSolidDownloadsUrl = "https://faq.topsolid.com/content/1/61/fr/ou-puis_je-trouver-les-installations-de-topsolid-version-actuelles-et-anciennes-versions-.html";

    public static LauncherCatalog Create()
    {
        var group = new LauncherGroup { Name = "Environnements Idéo", SortOrder = 0 };
        var workTag = NewTag("Travail", "Usage", "#2E69B3", 0, 0);
        var topSolidTag = NewTag("TopSolid'Wood", "Application", "#52667D", 5, 0);
        var yearTag = NewTag("2021", "Année", "#6B5BA7", 3, 0);
        var epTag = NewTag("EP", "Chants", "#C98247", 2, 0, "Avec gestion des chants");
        var scTag = NewTag("SC", "Chants", "#4F8A6D", 2, 1, "Sans gestion des chants");
        var tags = new List<LauncherTag> { workTag, topSolidTag, yearTag, epTag, scTag };
        var cards = new List<LauncherCard>();

        foreach (var installation in TopSolidDiscovery.FindInstallations())
        {
            var versionTag = NewTag(installation.Version, "Version", "#C98247", 1, tags.Count);
            tags.Add(versionTag);
            var profile = new LauncherLaunchProfile
            {
                Name = "Par défaut",
                TargetPath = installation.ExecutablePath,
                Arguments = installation.Arguments,
                WorkingDirectory = installation.FolderPath,
                MinimizeAfterLaunch = true
            };
            cards.Add(new LauncherCard
            {
                Title = $"TopSolid'Wood {installation.Version}",
                Subtitle = "Base de travail",
                GroupId = group.Id,
                DefaultLaunchProfileId = profile.Id,
                LaunchProfiles = [profile],
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
            Cards = cards,
            Resources = CreateDefaultResources()
        };
    }

    public static List<LauncherResource> CreateDefaultResources() =>
    [
        new LauncherResource
        {
            Id = TopSolidDownloadsResourceId,
            Name = "Téléchargements TopSolid",
            IconGlyph = "\uE774",
            SortOrder = 0,
            DefaultLocationId = TopSolidDownloadsLocationId,
            Locations =
            [
                new LauncherResourceLocation
                {
                    Id = TopSolidDownloadsLocationId,
                    Name = "Site TopSolid",
                    Target = TopSolidDownloadsUrl,
                    SortOrder = 0
                }
            ]
        },
        new LauncherResource
        {
            Id = TopSolidLicensesResourceId,
            Name = "Licences TopSolid",
            IconGlyph = "\uE8D7",
            SortOrder = 1,
            DefaultLocationId = TopSolidLicensesLocalId,
            Locations =
            [
                new LauncherResourceLocation { Id = TopSolidLicensesLocalId, Name = "Local", SortOrder = 0 },
                new LauncherResourceLocation { Id = TopSolidLicensesRemoteId, Name = "Distant · Nextcloud", SortOrder = 1 }
            ]
        }
    ];

    private static LauncherTag NewTag(string name, string category, string color, int categorySortOrder, int sortOrder, string description = "") => new()
    {
        Name = name,
        Category = category,
        Description = description,
        Color = color,
        CategorySortOrder = categorySortOrder,
        SortOrder = sortOrder
    };
}
