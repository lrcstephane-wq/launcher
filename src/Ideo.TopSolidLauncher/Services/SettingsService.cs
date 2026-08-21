using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Services;

public sealed class SettingsService
{
    public UserSettings Load()
    {
        try
        {
            var settings = JsonFileService.Read<UserSettings>(AppPaths.SettingsPath) ?? new UserSettings();
            settings.WindowWidth = double.IsFinite(settings.WindowWidth) ? Math.Clamp(settings.WindowWidth, 980, 3840) : 1280;
            settings.WindowHeight = double.IsFinite(settings.WindowHeight) ? Math.Clamp(settings.WindowHeight, 650, 2160) : 820;
            settings.FavoriteCardIds = settings.FavoriteCardIds?.Distinct().ToList() ?? [];
            settings.RecentCardIds = settings.RecentCardIds?.Distinct().Take(20).ToList() ?? [];
            settings.CollapsedGroupIds = settings.CollapsedGroupIds?.Distinct().ToList() ?? [];
            settings.SavedViews ??= [];
            foreach (var view in settings.SavedViews)
                view.SelectedTagIds = view.SelectedTagIds?.Distinct().ToList() ?? [];
            return settings;
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
