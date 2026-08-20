using System.IO;

namespace Ideo.TopSolidLauncher;

public sealed record TopSolidInstallation(string Version, string ExecutablePath)
{
    public string DisplayName => $"TopSolid'Wood {Version}";
    public string FolderPath => Path.GetDirectoryName(ExecutablePath) ?? string.Empty;
}
