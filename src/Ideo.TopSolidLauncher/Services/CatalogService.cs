using System.IO;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public sealed class CatalogService
{
    private const int BackupRetention = 20;
    private DateTime? _knownWriteUtc;

    public string CatalogPath { get; private set; }

    public CatalogService(string? configuredPath)
    {
        CatalogPath = string.IsNullOrWhiteSpace(configuredPath)
            ? AppPaths.DefaultCatalogPath
            : Environment.ExpandEnvironmentVariables(configuredPath.Trim());
    }

    public LauncherCatalog LoadOrCreate()
    {
        if (!File.Exists(CatalogPath))
        {
            var catalog = DefaultCatalogFactory.Create();
            Save(catalog, createBackup: false);
            return catalog;
        }

        var loaded = JsonFileService.Read<LauncherCatalog>(CatalogPath)
            ?? throw new InvalidDataException("Le catalogue ne contient pas de données valides.");
        Normalize(loaded);
        _knownWriteUtc = File.GetLastWriteTimeUtc(CatalogPath);
        return loaded;
    }

    public void Save(LauncherCatalog catalog, bool createBackup = true)
    {
        Normalize(catalog);
        catalog.UpdatedUtc = DateTime.UtcNow;

        if (createBackup && File.Exists(CatalogPath) && _knownWriteUtc is not null &&
            File.GetLastWriteTimeUtc(CatalogPath) > _knownWriteUtc.Value.AddMilliseconds(100))
        {
            throw new InvalidOperationException(
                "Le catalogue a été modifié par un autre poste. Rechargez-le avant d'enregistrer afin de ne pas écraser ses changements.");
        }

        if (createBackup && File.Exists(CatalogPath))
            CreateBackup();

        JsonFileService.WriteAtomic(CatalogPath, catalog);
        _knownWriteUtc = File.GetLastWriteTimeUtc(CatalogPath);
    }

    public void UseCatalog(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Le chemin du catalogue ne peut pas être vide.", nameof(path));
        CatalogPath = Environment.ExpandEnvironmentVariables(path.Trim());
        _knownWriteUtc = null;
    }

    public string ImportCardLogo(LauncherCard card)
    {
        if (string.IsNullOrWhiteSpace(card.LogoPath))
            return string.Empty;

        var source = ResolveAssetPath(card.LogoPath);
        if (!File.Exists(source))
            return card.LogoPath;
        if (!Path.IsPathRooted(card.LogoPath))
            return card.LogoPath;

        var extension = Path.GetExtension(source).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico"))
            return string.Empty;

        var relativePath = Path.Combine("Assets", $"{card.Id:N}-{DateTime.UtcNow.Ticks:x}{extension}");
        var destination = Path.Combine(Path.GetDirectoryName(CatalogPath)!, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            File.Copy(source, destination, overwrite: true);
        return relativePath;
    }

    public string ResolveAssetPath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        return Path.IsPathRooted(expanded)
            ? expanded
            : Path.Combine(Path.GetDirectoryName(CatalogPath)!, expanded);
    }

    public void CreateCopyAt(string destinationPath, LauncherCatalog catalog)
    {
        if (string.Equals(Path.GetFullPath(destinationPath), Path.GetFullPath(CatalogPath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choisissez un emplacement différent du catalogue actuel.");
        var destinationFolder = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("Le dossier de destination est introuvable.");
        Directory.CreateDirectory(destinationFolder);

        foreach (var card in catalog.Cards.Where(card => !string.IsNullOrWhiteSpace(card.LogoPath) && !Path.IsPathRooted(card.LogoPath)))
        {
            var source = ResolveAssetPath(card.LogoPath);
            if (!File.Exists(source)) continue;
            var destination = Path.Combine(destinationFolder, card.LogoPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
        }

        JsonFileService.WriteAtomic(destinationPath, catalog);
    }

    public void Export(LauncherCatalog catalog, string destinationPath) =>
        JsonFileService.WriteAtomic(destinationPath, catalog);

    public LauncherCatalog Import(string sourcePath)
    {
        var catalog = JsonFileService.Read<LauncherCatalog>(sourcePath)
            ?? throw new InvalidDataException("Le fichier sélectionné n'est pas un catalogue valide.");
        Normalize(catalog);
        return catalog;
    }

    private void CreateBackup()
    {
        var folder = Path.Combine(Path.GetDirectoryName(CatalogPath)!, "Backups");
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, $"catalog-{DateTime.Now:yyyyMMdd-HHmmss-fff}.json");
        File.Copy(CatalogPath, destination, overwrite: false);

        foreach (var oldBackup in Directory.EnumerateFiles(folder, "catalog-*.json")
                     .OrderByDescending(File.GetCreationTimeUtc)
                     .Skip(BackupRetention))
        {
            try
            {
                File.Delete(oldBackup);
            }
            catch (Exception exception)
            {
                LogService.Write($"Impossible de supprimer l'ancienne sauvegarde « {oldBackup} ».", exception);
            }
        }
    }

    private static void Normalize(LauncherCatalog catalog)
    {
        if (catalog.SchemaVersion > LauncherCatalog.CurrentSchemaVersion)
            throw new InvalidDataException(
                $"Ce catalogue utilise le format {catalog.SchemaVersion}, plus récent que celui pris en charge par cette version du launcher.");

        catalog.SchemaVersion = LauncherCatalog.CurrentSchemaVersion;
        catalog.Groups ??= [];
        catalog.Tags ??= [];
        catalog.Cards ??= [];

        EnsureUniqueIds(catalog.Groups.Select(group => group.Id), "groupes");
        EnsureUniqueIds(catalog.Tags.Select(tag => tag.Id), "tags");
        EnsureUniqueIds(catalog.Cards.Select(card => card.Id), "cartes");

        if (catalog.Groups.Count == 0)
            catalog.Groups.Add(new LauncherGroup { Name = "Mes applications" });

        foreach (var group in catalog.Groups)
            group.Name = string.IsNullOrWhiteSpace(group.Name) ? "Groupe sans nom" : group.Name.Trim();

        foreach (var tag in catalog.Tags)
        {
            tag.Name = string.IsNullOrWhiteSpace(tag.Name) ? "Tag sans nom" : tag.Name.Trim();
            tag.Category = string.IsNullOrWhiteSpace(tag.Category) ? "Autre" : tag.Category.Trim();
            tag.Description = tag.Description?.Trim() ?? string.Empty;
            tag.Color = NormalizeColor(tag.Color, "#315D9D");
        }

        var defaultGroup = catalog.Groups.OrderBy(group => group.SortOrder).First().Id;
        foreach (var card in catalog.Cards)
        {
            card.Title = string.IsNullOrWhiteSpace(card.Title) ? "Application sans titre" : card.Title.Trim();
            card.Subtitle = card.Subtitle?.Trim() ?? string.Empty;
            card.TargetPath = card.TargetPath?.Trim() ?? string.Empty;
            card.Arguments = card.Arguments?.Trim() ?? string.Empty;
            card.WorkingDirectory = card.WorkingDirectory?.Trim() ?? string.Empty;
            card.LogoPath = card.LogoPath?.Trim() ?? string.Empty;
            card.AccentColor = NormalizeColor(card.AccentColor, "#2E69B3");
            card.GroupId = catalog.Groups.Any(group => group.Id == card.GroupId) ? card.GroupId : defaultGroup;
            card.TagIds ??= [];
            card.TagIds = card.TagIds.Distinct().Where(id => catalog.Tags.Any(tag => tag.Id == id)).ToList();
        }
    }

    private static void EnsureUniqueIds(IEnumerable<Guid> ids, string collectionName)
    {
        var values = ids.ToArray();
        if (values.Any(id => id == Guid.Empty) || values.Distinct().Count() != values.Length)
            throw new InvalidDataException($"Le catalogue contient des identifiants invalides ou dupliqués dans les {collectionName}.");
    }

    private static string NormalizeColor(string? color, string fallback)
    {
        if (string.IsNullOrWhiteSpace(color) || color[0] != '#')
            return fallback;
        var value = color[1..];
        return value.Length is 6 or 8 && uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out _)
            ? $"#{value.ToUpperInvariant()}"
            : fallback;
    }
}
