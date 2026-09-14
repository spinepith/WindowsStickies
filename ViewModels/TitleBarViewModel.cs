using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;


namespace WindowsStickies.ViewModels;

public partial class TitleBarViewModel : BaseTitleBarViewModel {
    private MainViewModel? _mainViewModel;

    public string PinIconPath => IsTopmost ? "/Assets/pinned.svg" : "/Assets/pin.svg";
    public Services.SettingsService Settings => Services.SettingsService.Instance;

    public System.Action<string>? EditorAction;

    public ObservableCollection<LanguageItem> AvailableLanguages { get; }

    public Models.StickyModel Model { get; }


    public TitleBarViewModel(Models.StickyModel model, MainViewModel? mainViewModel = null) {
        Model = model;
        _mainViewModel = mainViewModel;
        IsTopmost = model.IsTopmost;

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
        if (Model is not null)
            Model.IsTopmost = IsTopmost;
    }
    #endregion

    #region NEW STICKY
    [RelayCommand]
    private void NewSticky() {
        if (Model is not null)
            WeakReferenceMessenger.Default.Send(new Models.NewStickyMessage { SourceX = Model.X, SourceY = Model.Y });
        else
            WeakReferenceMessenger.Default.Send(new Models.NewStickyMessage());
    }
    #endregion

    #region FILE
    [RelayCommand]
    private void ImportText() {

    }

    [RelayCommand]
    private void ExportText() {

    }

    [RelayCommand]
    private void RuledLines() {

    }
    #endregion

    #region EDIT
    [RelayCommand]
    private void Cut() {

    }

    [RelayCommand]
    private void Copy() {

    }

    [RelayCommand]
    private void Paste() {

    }

    [RelayCommand]
    private void PasteWithoutFormatting() {

    }

    [RelayCommand]
    private void DeleteText() {

    }

    [RelayCommand]
    private void SelectAll() {

    }

    [RelayCommand]
    private void Find() {
        WeakReferenceMessenger.Default.Send(new Models.OpenFindTextMessage { SourceViewModel = _mainViewModel });
    }
    #endregion

    #region FONT
    [RelayCommand]
    private void ShowAllFonts() {
        WeakReferenceMessenger.Default.Send(new Models.OpenFontPickerMessage { SourceViewModel = _mainViewModel });
    }

    [RelayCommand]
    private void SetFontStyle(string styleName) {
        switch (styleName) {
            case "Hyperlink":
                WeakReferenceMessenger.Default.Send(new Models.OpenHyperlinkMessage { SourceViewModel = _mainViewModel });
                break;
        }
    }

    [RelayCommand]
    private void IncreaseFontSize() {
        EditorAction?.Invoke(nameof(IncreaseFontSize));
    }

    [RelayCommand]
    private void DecreaseFontSize() {
        EditorAction?.Invoke(nameof(DecreaseFontSize));
    }

    [RelayCommand]
    private void ZoomIn() {
        if (Model.Zoom < 4.0)
            Model.Zoom += 0.1;
    }

    [RelayCommand]
    private void ZoomOut() {
        if (Model.Zoom > 0.3)
            Model.Zoom -= 0.1;
    }

    [RelayCommand]
    private void FontColor() {
        WeakReferenceMessenger.Default.Send(new Models.OpenColorPickerMessage { SourceViewModel = _mainViewModel, Mode = Models.OpenColorPickerMessage.PickerMode.FontColor });
    }

    [RelayCommand]
    private void HighlightColor() {
        WeakReferenceMessenger.Default.Send(new Models.OpenColorPickerMessage { SourceViewModel = _mainViewModel, Mode = Models.OpenColorPickerMessage.PickerMode.HighlightColor });
    }
    #endregion

    #region COLOR
    [RelayCommand]
    private void BackgroundColor(string? hexColor) {
        if (!string.IsNullOrEmpty(hexColor))
            Model.BackgroundColor = hexColor;
        else
            WeakReferenceMessenger.Default.Send(new Models.OpenColorPickerMessage { SourceViewModel = _mainViewModel, Mode = Models.OpenColorPickerMessage.PickerMode.BackgroundColor });
    }
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
