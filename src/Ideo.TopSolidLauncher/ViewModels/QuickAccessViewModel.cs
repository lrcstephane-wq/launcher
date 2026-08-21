using System.IO;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class QuickAccessViewModel
{
    private readonly string _catalogFolder;

    public LauncherQuickAccess Model { get; }
    public string Label => Model.Label;
    public string DisplayPath => Model.Path;
    public string ResolvedPath => Resolve(Model.Path);
    public bool IsAvailable => Directory.Exists(ResolvedPath);

    public QuickAccessViewModel(LauncherQuickAccess model, string catalogFolder)
    {
        Model = model;
        _catalogFolder = catalogFolder;
    }

    private string Resolve(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        return Path.IsPathRooted(expanded) ? expanded : Path.Combine(_catalogFolder, expanded);
    }
}
