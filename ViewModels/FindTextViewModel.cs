using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowsStickies.ViewModels; 

public partial class FindTextViewModel : BaseTitleBarViewModel {
    
    [ObservableProperty]
    private MainViewModel? _targetViewModel;

    public FindTextViewModel(MainViewModel? targetViewModel) {
        TargetViewModel = targetViewModel;
    }
}
