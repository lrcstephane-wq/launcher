using System.Text.Json;
using Ideo.TopSolidLauncher.Services;

var root = Path.Combine(Path.GetTempPath(), $"ideo-launcher-migration-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
var catalogPath = Path.Combine(root, "catalog.json");

const string cardId = "d2ece9dd-64f4-48a6-a050-b73f2bde8f13";
const string groupId = "87cd757c-d066-4550-9dd0-330f6a917823";
const string tagId = "30d50f33-02e1-48c2-82a4-f75a3d0ec9af";

var legacyJson = $$"""
{
  "SchemaVersion": 2,
  "UpdatedUtc": "2026-10-01T08:00:00Z",
  "Groups": [{ "Id": "{{groupId}}", "Name": "Clients", "SortOrder": 0 }],
  "Tags": [{ "Id": "{{tagId}}", "Name": "Travail", "Category": "Usage", "Description": "Base active", "Color": "#2E69B3", "SortOrder": 0 }],
  "Cards": [{
    "Id": "{{cardId}}",
    "Title": "Carte conservée",
    "Subtitle": "Configuration Thomas",
    "GroupId": "{{groupId}}",
    "TargetPath": "C:\\Missler\\V627\\bin\\TopSolid.exe",
    "Arguments": "-a test",
    "WorkingDirectory": "C:\\Missler\\V627\\bin",
    "LogoPath": "Assets\\client.png",
    "AccentColor": "#4F8A6D",
    "QuickAccessLinks": [{ "Label": "Cfg", "Path": "C:\\Cfg" }],
    "TagIds": ["{{tagId}}"],
    "SortOrder": 7,
    "RunAsAdministrator": true,
    "MinimizeAfterLaunch": true
  }]
}
""";

try
{
    File.WriteAllText(catalogPath, legacyJson);
    var service = new CatalogService(catalogPath);
    var catalog = service.LoadOrCreate();
    Assert(catalog.SchemaVersion == 4, "Le schéma n'a pas été migré vers la version 4.");
    Assert(catalog.Cards.Count == 1, "La carte existante a été perdue ou dupliquée.");
    var card = catalog.Cards[0];
    Assert(card.Id == Guid.Parse(cardId), "L'identifiant de carte a changé.");
    Assert(card.Title == "Carte conservée" && card.Subtitle == "Configuration Thomas", "Les informations visibles ont changé.");
    Assert(card.GroupId == Guid.Parse(groupId) && card.TagIds.SequenceEqual([Guid.Parse(tagId)]), "Le groupe ou les tags ont changé.");
    Assert(card.SortOrder == 7 && card.LogoPath == "Assets\\client.png" && card.AccentColor == "#4F8A6D", "L'ordre ou l'apparence a changé.");
    Assert(card.LaunchProfiles.Count == 1, "L'ancienne commande n'a pas produit un mode unique.");
    var profile = card.LaunchProfiles[0];
    Assert(profile.Id == card.Id, "L'identifiant du mode migré n'est pas stable.");
    Assert(profile.Name == "Par défaut", "Le mode migré n'est pas nommé « Par défaut ».");
    Assert(profile.TargetPath.EndsWith("TopSolid.exe") && profile.Arguments == "-a test", "La cible ou les arguments ont changé.");
    Assert(profile.WorkingDirectory.EndsWith("bin") && profile.RunAsAdministrator && profile.MinimizeAfterLaunch, "Les options de lancement ont changé.");
    Assert(profile.QuickAccessLinks.Count == 1 && profile.QuickAccessLinks[0].Label == "Cfg" && profile.QuickAccessLinks[0].Path == "C:\\Cfg", "Les accès rapides ont changé.");

    service.Save(catalog, auditAction: "Test de migration");
    Assert(Directory.EnumerateFiles(Path.Combine(root, "Backups"), "catalog-*.json").Any(), "La sauvegarde de l'ancien catalogue n'a pas été créée.");
    using var json = JsonDocument.Parse(File.ReadAllText(catalogPath));
    Assert(json.RootElement.GetProperty("SchemaVersion").GetInt32() == 4, "Le catalogue enregistré n'utilise pas le schéma 4.");
    Assert(json.RootElement.GetProperty("Cards")[0].GetProperty("LaunchProfiles").GetArrayLength() == 1, "Le mode migré n'est pas présent dans le JSON.");
    Console.WriteLine("Migration v0.3.2 -> v0.4.0 : OK");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
