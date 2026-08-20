using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public sealed class SettingsService
{
    public UserSettings Load()
    {
        try
        {
            return JsonFileService.Read<UserSettings>(AppPaths.SettingsPath) ?? new UserSettings();
        }
        catch (Exception exception)
        {
            LogService.Write("Impossible de lire les préférences locales. Les valeurs par défaut sont utilisées.", exception);
            return new UserSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        JsonFileService.WriteAtomic(AppPaths.SettingsPath, settings);
    }
}
