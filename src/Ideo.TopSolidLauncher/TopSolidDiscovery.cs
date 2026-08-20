using System.Text.RegularExpressions;

namespace Ideo.TopSolidLauncher;

public static partial class TopSolidDiscovery
{
    private const string MisslerRoot = @"C:\Missler";

    // Different V6 deployments use one of these executable names.
    private static readonly string[] ExecutableCandidates =
    [
        "TopSolid.exe",
        "TopSolid'Wood.exe",
        "TopSolidWood.exe"
    ];

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

        var binFolder = Path.Combine(versionFolder, "bin");
        var executable = ExecutableCandidates
            .Select(name => Path.Combine(binFolder, name))
            .FirstOrDefault(File.Exists);

        if (executable is null)
        {
            executable = Directory.Exists(binFolder)
                ? Directory.EnumerateFiles(binFolder, "TopSolid*.exe", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(path => !Path.GetFileName(path).Contains("Update", StringComparison.OrdinalIgnoreCase))
                : null;
        }

        return executable is null
            ? null
            : new TopSolidInstallation($"6.{match.Groups[1].Value}", executable);
    }

    private static int ParseVersion(string version) =>
        int.TryParse(version.Split('.').LastOrDefault(), out var number) ? number : 0;

    [GeneratedRegex(@"^V6(\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex VersionFolderRegex();
}
