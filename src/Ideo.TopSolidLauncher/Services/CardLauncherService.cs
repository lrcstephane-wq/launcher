using System.Diagnostics;
using System.IO;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public sealed class CardLauncherService
{
    public Process? Launch(LauncherCard card)
    {
        var validation = CardValidationService.Validate(card);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);

        var target = Environment.ExpandEnvironmentVariables(card.TargetPath.Trim().Trim('"'));
        var workingDirectory = string.IsNullOrWhiteSpace(card.WorkingDirectory)
            ? Path.GetDirectoryName(target) ?? string.Empty
            : Environment.ExpandEnvironmentVariables(card.WorkingDirectory.Trim().Trim('"'));

        var startInfo = new ProcessStartInfo
        {
            FileName = target,
            Arguments = Environment.ExpandEnvironmentVariables(card.Arguments ?? string.Empty),
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };
        if (card.RunAsAdministrator)
            startInfo.Verb = "runas";

        LogService.Write($"Commande lancée : {target} {startInfo.Arguments}".TrimEnd());
        return Process.Start(startInfo);
    }
}
