using System.IO;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public static class CardValidationService
{
    public static CardValidationResult Validate(LauncherCard card)
    {
        if (string.IsNullOrWhiteSpace(card.Title))
            return CardValidationResult.Invalid("Le titre est obligatoire.");
        if (string.IsNullOrWhiteSpace(card.TargetPath))
            return CardValidationResult.Invalid("La cible est obligatoire.");
        if (!IsLaunchableTarget(card.TargetPath))
            return CardValidationResult.Invalid("La cible est introuvable. Vérifiez son chemin.");
        if (!string.IsNullOrWhiteSpace(card.WorkingDirectory) &&
            !Directory.Exists(Environment.ExpandEnvironmentVariables(card.WorkingDirectory)))
            return CardValidationResult.Invalid("Le dossier de travail est introuvable.");
        return CardValidationResult.Valid();
    }

    public static bool IsLaunchableTarget(string target)
    {
        var expanded = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));
        return File.Exists(expanded) ||
               Uri.TryCreate(expanded, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }
}
