using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ideo.TopSolidLauncher.Models;

public sealed class LauncherQuickAccess : INotifyPropertyChanged
{
    private string _label = "Dossier";
    private string _path = string.Empty;
    public string Label { get => _label; set { if (_label == value) return; _label = value; OnPropertyChanged(); } }
    public string Path { get => _path; set { if (_path == value) return; _path = value; OnPropertyChanged(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public LauncherQuickAccess Clone() => new()
    {
        Label = Label,
        Path = Path
    };
}
