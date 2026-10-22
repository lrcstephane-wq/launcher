using System.IO;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public static class CardValidationService
{
    public static CardValidationResult ValidateForSave(LauncherCard card)
    {
        if (string.IsNullOrWhiteSpace(card.Title))
            return CardValidationResult.Invalid("Le titre est obligatoire.");
        if (card.LaunchProfiles.Count == 0)
            return CardValidationResult.Invalid("Ajoutez au moins un mode de lancement.");
        if (card.LaunchProfiles.Select(profile => profile.Name.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).Count() != card.LaunchProfiles.Count)
            return CardValidationResult.Invalid("Chaque mode de lancement doit avoir un nom différent.");
        foreach (var profile in card.LaunchProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
                return CardValidationResult.Invalid("Chaque mode de lancement doit avoir un nom.");
            if (string.IsNullOrWhiteSpace(profile.TargetPath))
                return CardValidationResult.Invalid($"La cible du mode « {profile.Name} » est obligatoire.");
            if (profile.QuickAccessLinks.Any(link => string.IsNullOrWhiteSpace(link.Label) || string.IsNullOrWhiteSpace(link.Path)))
                return CardValidationResult.Invalid($"Chaque accès rapide du mode « {profile.Name} » doit avoir un nom et un chemin.");
        }
        return CardValidationResult.Valid();
    }

    public static CardValidationResult Validate(LauncherCard card, LauncherLaunchProfile? profile = null)
    {
        var structure = ValidateForSave(card);
        if (!structure.IsValid)
            return structure;
        return ValidateProfile(profile ?? card.LaunchProfiles.FirstOrDefault(item => item.Id == card.DefaultLaunchProfileId) ?? card.LaunchProfiles[0]);
    }

    public static CardValidationResult ValidateProfile(LauncherLaunchProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.TargetPath))
            return CardValidationResult.Invalid("La cible est obligatoire.");
        if (!IsLaunchableTarget(profile.TargetPath))
            return CardValidationResult.Invalid("La cible est introuvable. Vérifiez son chemin.");
        if (!string.IsNullOrWhiteSpace(profile.WorkingDirectory) &&
            !Directory.Exists(Environment.ExpandEnvironmentVariables(profile.WorkingDirectory.Trim().Trim('"'))))
            return CardValidationResult.Invalid("Le dossier de travail est introuvable.");
        return CardValidationResult.Valid();
    }

    public static bool IsLaunchableTarget(string target)
    {
        var expanded = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));
        return File.Exists(expanded) ||
               Uri.TryCreate(expanded, UriKind.Absolute, out var uri) && !uri.IsFile && !string.IsNullOrWhiteSpace(uri.Scheme);
    }
}
