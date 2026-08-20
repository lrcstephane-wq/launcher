using System.Collections.ObjectModel;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class FilterCategoryViewModel
{
    public string Name { get; }
    public ObservableCollection<FilterOptionViewModel> Options { get; }

    public FilterCategoryViewModel(string name, IEnumerable<FilterOptionViewModel> options)
    {
        Name = name;
        Options = new ObservableCollection<FilterOptionViewModel>(options);
    }
}
