using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;


namespace WindowsStickies.ViewModels;

public partial class TitleBarViewModel : BaseTitleBarViewModel {
    public string PinIconPath => IsTopmost ? "/Assets/pinned.svg" : "/Assets/pin.svg";

    public ObservableCollection<LanguageItem> AvailableLanguages { get; } = new() {
        new LanguageItem { Name = "Русский", Code = "ru" },
        new LanguageItem { Name = "English", Code = "en" },
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinIconPath))]
    private bool _isTopmost = false;

    #region TITLE BAR
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
    [RelayCommand]
    private void ShowAbout() {
        WeakReferenceMessenger.Default.Send(new Models.OpenAboutMessage());
    }
    #endregion
}

public class LanguageItem {
    public string? Name { get; set; }
    public string? Code { get; set; }
}
