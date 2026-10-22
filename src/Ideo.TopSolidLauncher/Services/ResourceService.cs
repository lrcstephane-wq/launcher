using System.Diagnostics;
using System.IO;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public static class ResourceService
{
    public static CardValidationResult Validate(LauncherResourceLocation location)
    {
        if (string.IsNullOrWhiteSpace(location.Target))
            return CardValidationResult.Invalid("Cet emplacement n'est pas encore configuré.");
        var target = ResolveTarget(location.Target);
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && !uri.IsFile)
            return CardValidationResult.Valid();
        if (Directory.Exists(target) || File.Exists(target))
            return CardValidationResult.Valid();
        return CardValidationResult.Invalid("Le chemin est introuvable ou inaccessible.");
    }

    public static void Open(LauncherResource resource, LauncherResourceLocation location)
    {
        var validation = Validate(location);
        if (!validation.IsValid)
            throw new InvalidOperationException($"{validation.Message}\n\n{ResolveTarget(location.Target)}");

        var target = ResolveTarget(location.Target);
        LogService.Write($"Ressource ouverte : {resource.Name} · {location.Name} · {target}");
        Process.Start(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }

    public static string ResolveTarget(string target) =>
        Environment.ExpandEnvironmentVariables((target ?? string.Empty).Trim().Trim('"'));
}
