using System.IO;
using System.Text.RegularExpressions;

namespace Ideo.TopSolidLauncher;

public static partial class TopSolidDiscovery
{
    private const string MisslerRoot = @"C:\Missler";

    public static IReadOnlyList<TopSolidInstallation> FindInstallations()
    {
        if (!Directory.Exists(MisslerRoot))
            return [];

        return Directory.EnumerateDirectories(MisslerRoot, "V6*", SearchOption.TopDirectoryOnly)
            .Select(TryCreateInstallation)
            .Where(installation => installation is not null)
            .Cast<TopSolidInstallation>()
            .OrderByDescending(installation => ParseVersion(installation.Version))
            .ToArray();
    }

    private static TopSolidInstallation? TryCreateInstallation(string versionFolder)
    {
        var folderName = Path.GetFileName(versionFolder);
        var match = VersionFolderRegex().Match(folderName);
        if (!match.Success)
            return null;

        var versionNumber = match.Groups[1].Value;
        var displayVersion = $"6.{versionNumber}";
        var binFolder = Path.Combine(versionFolder, "bin");
        var executable = Path.Combine(binFolder, $"top6{versionNumber}.exe");

        return !File.Exists(executable)
            ? null
            : new TopSolidInstallation(
                displayVersion,
                executable,
                $"-fTOPSOLID/217/{displayVersion}");
    }

    private static int ParseVersion(string version) =>
        int.TryParse(version.Split('.').LastOrDefault(), out var number) ? number : 0;

    [GeneratedRegex(@"^V6(\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex VersionFolderRegex();
}
