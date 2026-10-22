using System.Diagnostics;
using System.IO;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public sealed class CardLauncherService
{
    public Process? Launch(LauncherCard card, LauncherLaunchProfile profile)
    {
        var validation = CardValidationService.ValidateProfile(profile);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);

        var target = Environment.ExpandEnvironmentVariables(profile.TargetPath.Trim().Trim('"'));
        var isUri = Uri.TryCreate(target, UriKind.Absolute, out var uri) && !uri.IsFile;
        var workingDirectory = string.IsNullOrWhiteSpace(profile.WorkingDirectory)
            ? isUri ? AppContext.BaseDirectory : Path.GetDirectoryName(target) ?? AppContext.BaseDirectory
            : Environment.ExpandEnvironmentVariables(profile.WorkingDirectory.Trim().Trim('"'));

        var startInfo = new ProcessStartInfo
        {
            FileName = target,
            Arguments = Environment.ExpandEnvironmentVariables(profile.Arguments ?? string.Empty),
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };
        if (profile.RunAsAdministrator)
            startInfo.Verb = "runas";

        LogService.Write($"Commande lancée : {card.Title} · {profile.Name} · {target} {startInfo.Arguments}".TrimEnd());
        return Process.Start(startInfo);
    }
}
