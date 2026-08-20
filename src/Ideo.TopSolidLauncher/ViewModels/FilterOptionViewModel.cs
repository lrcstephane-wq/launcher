using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class FilterOptionViewModel : ObservableObject
{
    private bool _isSelected;

    public LauncherTag Tag { get; }
    public string Name => Tag.Name;
    public string Description => string.IsNullOrWhiteSpace(Tag.Description) ? $"{Tag.Category} · {Tag.Name}" : Tag.Description;
    public string Color => Tag.Color;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectionChanged;

    public FilterOptionViewModel(LauncherTag tag) => Tag = tag;
}
