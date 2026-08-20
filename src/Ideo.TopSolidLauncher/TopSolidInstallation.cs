using System.IO;

namespace Ideo.TopSolidLauncher;

public sealed record TopSolidInstallation(string Version, string ExecutablePath, string Arguments)
{
    public string DisplayName => $"TopSolid'Wood {Version}";
    public string FolderPath => Path.GetDirectoryName(ExecutablePath) ?? string.Empty;
    public string CommandLine => $"{ExecutablePath} {Arguments}";
}
