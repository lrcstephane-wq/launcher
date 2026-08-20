using System.IO;

namespace Ideo.TopSolidLauncher.Services;

public static class AppPaths
{
    public static string RoamingFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Ideo",
        "Launcher");

    public static string LocalFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ideo",
        "Launcher");

    public static string DefaultCatalogPath { get; } = Path.Combine(RoamingFolder, "catalog.json");
    public static string SettingsPath { get; } = Path.Combine(LocalFolder, "settings.json");
}
