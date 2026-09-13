using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowsStickies.ViewModels;

public partial class MainViewModel : ViewModelBase {
    public TitleBarViewModel TitleBar { get; }
    public Models.StickyModel StickyModel { get; }

    [ObservableProperty]
    private bool _hasSelection = false;

    [ObservableProperty]
    private string _selectedText = string.Empty;

    [ObservableProperty]
    private string _selectedHyperlinkUrl = "";

    [ObservableProperty]
    private string _selectedFontColor = "#FF000000";

    [ObservableProperty]
    private string _selectedHighlightColor = "#00000000";

    public MainViewModel(Models.StickyModel model) {
        StickyModel = model;
        TitleBar = new TitleBarViewModel(model, this);
    }
}
