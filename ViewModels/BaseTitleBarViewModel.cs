using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace WindowsStickies.ViewModels;

public partial class BaseTitleBarViewModel : ViewModelBase {
    [ObservableProperty]
    private WindowState _windowState = WindowState.Normal;

    [RelayCommand]
    private void Minimize(Window? window) {
        if (window != null)
            window.WindowState = WindowState.Minimized;
    }

    [RelayCommand]
    private void Close(Window window) {
        window.Close();
    }
}
