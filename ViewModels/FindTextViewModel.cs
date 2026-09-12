using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WindowsStickies.Models;

namespace WindowsStickies.ViewModels; 

public partial class FindTextViewModel : BaseTitleBarViewModel {
    
    [ObservableProperty]
    private MainViewModel? _targetViewModel;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public FindTextViewModel(MainViewModel? targetViewModel) {
        TargetViewModel = targetViewModel;
    }

    [RelayCommand]
    private void FindNext() {
        if (string.IsNullOrEmpty(SearchText) || TargetViewModel is null)
            return;

        WeakReferenceMessenger.Default.Send(new FindRequestMessage(TargetViewModel, SearchText));
    }
}
