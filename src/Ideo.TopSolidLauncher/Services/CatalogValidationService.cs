using System.IO;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public static class CatalogValidationService
{
    public static IReadOnlyList<CatalogValidationIssue> Validate(LauncherCatalog catalog, string catalogFolder)
    {
        var issues = new List<CatalogValidationIssue>();
        foreach (var card in catalog.Cards)
        {
            var structure = CardValidationService.ValidateForSave(card);
            if (!structure.IsValid)
            {
                issues.Add(CardIssue(card, string.Empty, structure.Message));
                continue;
            }

            foreach (var profile in card.LaunchProfiles.OrderBy(item => item.SortOrder))
            {
                var result = CardValidationService.ValidateProfile(profile);
                if (!result.IsValid)
                    issues.Add(CardIssue(card, profile.Name, result.Message));
                foreach (var access in profile.QuickAccessLinks)
                {
                    var path = ResolvePath(access.Path, catalogFolder);
                    if (!Directory.Exists(path))
                        issues.Add(CardIssue(card, $"{profile.Name} · {access.Label}", $"Dossier introuvable : {path}"));
                }
            }
        }

        foreach (var resource in catalog.Resources)
        foreach (var location in resource.Locations.OrderBy(item => item.SortOrder))
        {
            var result = ResourceService.Validate(location);
            if (!result.IsValid)
                issues.Add(new CatalogValidationIssue
                {
                    Kind = CatalogIssueKind.Resource,
                    ItemId = resource.Id,
                    ItemName = resource.Name,
                    LocationName = location.Name,
                    Message = result.Message
                });
        }
        return issues;
    }

    private static CatalogValidationIssue CardIssue(LauncherCard card, string location, string message) => new()
    {
        Kind = CatalogIssueKind.Card,
        ItemId = card.Id,
        ItemName = card.Title,
        LocationName = location,
        Message = message
    };

    private static string ResolvePath(string path, string catalogFolder)
    {
        var expanded = Environment.ExpandEnvironmentVariables((path ?? string.Empty).Trim().Trim('"'));
        return Path.IsPathRooted(expanded) ? expanded : Path.Combine(catalogFolder, expanded);
    }
}
