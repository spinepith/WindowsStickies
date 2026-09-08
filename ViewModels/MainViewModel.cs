using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindowsStickies.ViewModels;

public partial class MainViewModel : ViewModelBase {
    public TitleBarViewModel TitleBar { get; }
    public Models.StickyModel Model { get; }

    public MainViewModel(Models.StickyModel model) {
        Model = model;
        TitleBar = new TitleBarViewModel(model);
    }
}
