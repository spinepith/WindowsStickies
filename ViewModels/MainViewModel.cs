using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindowsStickies.ViewModels;

public partial class MainViewModel : ViewModelBase {
    public TitleBarViewModel TitleBar { get; } = new TitleBarViewModel();
}
