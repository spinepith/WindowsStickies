using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;


namespace WindowsStickies.ViewModels;

public partial class TitleBarViewModel : BaseTitleBarViewModel {
    public string PinIconPath => IsTopmost ? "/Assets/pinned.svg" : "/Assets/pin.svg";
    public Services.SettingsService Settings => Services.SettingsService.Instance;

    public ObservableCollection<LanguageItem> AvailableLanguages { get; }

    public TitleBarViewModel() {
        AvailableLanguages = new ObservableCollection<LanguageItem> {
            new LanguageItem { Name = "Русский", Code = "ru", Command = ChangeLanguageCommand },
            new LanguageItem { Name = "English", Code = "en", Command = ChangeLanguageCommand }
        };
    }

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
    [RelayCommand]
    private void NewSticky() {
        WeakReferenceMessenger.Default.Send(new Models.NewNoteMessage());
    }
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
        Settings.LanguageCode = cultureCode;
    }
    #endregion

    #region CHANGE RADIUS
    [RelayCommand]
    private void ChangeCornerRadius() {
        Settings.IsRoundedCorners = !Settings.IsRoundedCorners;
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
    public System.Windows.Input.ICommand? Command { get; set; }
}
