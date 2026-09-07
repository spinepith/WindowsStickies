using System.Collections.ObjectModel;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WindowsStickies.ViewModels;

public partial class TitleBarViewModel : ViewModelBase {
    public string PinIconPath => IsTopmost ? "/Assets/pinned.svg" : "/Assets/pin.svg";

    public ObservableCollection<LanguageItem> AvailableLanguages { get; } = new() {
        new LanguageItem { Name = "Русский", Code = "ru" },
        new LanguageItem { Name = "English", Code = "en" },
    };

    [ObservableProperty]
    private WindowState _windowState = WindowState.Normal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinIconPath))]
    private bool _isTopmost = false;

    #region TITLE BAR
    [RelayCommand]
    private void Minimize() {
        WindowState = WindowState.Minimized;
    }

    [RelayCommand]
    private void ToggleTopmost() {
        IsTopmost = !IsTopmost;
    }
    #endregion

    #region NEW STICKY
    #endregion

    #region FILE
    #endregion

    #region EDIT
    #endregion

    #region FONT
    #endregion

    #region COLOR
    #endregion

    #region LANGUAGE
    [RelayCommand]
    private void ChangeLanguage(string cultureCode) {
        Locales.Localizer.Instance.SetLanguage(cultureCode);
    }
    #endregion

    #region ABOUT
    #endregion
}

public class LanguageItem {
    public string? Name { get; set; }
    public string? Code { get; set; }
}
