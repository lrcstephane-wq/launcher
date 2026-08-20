using System.Collections.ObjectModel;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class GroupViewModel
{
    public LauncherGroup Model { get; }
    public string Name => Model.Name;
    public ObservableCollection<CardViewModel> Cards { get; }
    public int CardCount => Cards.Count;
    public string CountLabel => Cards.Count == 1 ? "1 raccourci" : $"{Cards.Count} raccourcis";

    public GroupViewModel(LauncherGroup model, IEnumerable<CardViewModel> cards)
    {
        Model = model;
        Cards = new ObservableCollection<CardViewModel>(cards);
    }
}
