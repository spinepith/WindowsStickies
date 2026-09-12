namespace WindowsStickies.ViewModels;

public partial class MainViewModel : ViewModelBase {
    public TitleBarViewModel TitleBar { get; }
    public Models.StickyModel StickyModel { get; }

    public MainViewModel(Models.StickyModel model) {
        StickyModel = model;
        TitleBar = new TitleBarViewModel(model, this);
    }
}
