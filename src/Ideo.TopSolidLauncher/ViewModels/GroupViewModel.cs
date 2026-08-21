using System.Collections.ObjectModel;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class GroupViewModel : ObservableObject
{
    private bool _isCollapsed;
    public LauncherGroup Model { get; }
    public string Name => Model.Name;
    public ObservableCollection<CardViewModel> Cards { get; }
    public int CardCount => Cards.Count;
    public string CountLabel => Cards.Count == 1 ? "1 raccourci" : $"{Cards.Count} raccourcis";
    public string CollapseGlyph => IsCollapsed ? "›" : "⌄";
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (!SetProperty(ref _isCollapsed, value)) return;
            OnPropertyChanged(nameof(CollapseGlyph));
        }
    }

    public GroupViewModel(LauncherGroup model, IEnumerable<CardViewModel> cards, bool isCollapsed)
    {
        Model = model;
        Cards = new ObservableCollection<CardViewModel>(cards);
        _isCollapsed = isCollapsed;
    }
}
