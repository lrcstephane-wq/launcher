using System.Collections.ObjectModel;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class FilterCategoryViewModel
{
    public string Name { get; }
    public int SortOrder { get; }
    public ObservableCollection<FilterOptionViewModel> Options { get; }

    public FilterCategoryViewModel(string name, int sortOrder, IEnumerable<FilterOptionViewModel> options)
    {
        Name = name;
        SortOrder = sortOrder;
        Options = new ObservableCollection<FilterOptionViewModel>(options);
    }
}
