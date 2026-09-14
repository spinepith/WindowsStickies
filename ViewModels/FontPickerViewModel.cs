using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using WindowsStickies.Models;


namespace WindowsStickies.ViewModels; 

internal partial class FontPickerViewModel : BaseTitleBarViewModel {
    public IReadOnlyList<FontFamily> AvailableFonts { get; } = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();

    private bool _isApplied = false;

    [ObservableProperty]
    private MainViewModel? _targetViewModel;

    [ObservableProperty]
    private FontFamily? _selectedFont;

    [ObservableProperty]
    private double _selectedFontSize = 12;

    [RelayCommand]
    private void IncreaseSize() {
        if (SelectedFontSize < 100)
            SelectedFontSize++;
    }

    [RelayCommand]
    private void DecreaseSize() {
        if (SelectedFontSize > 8)
            SelectedFontSize--;
    }

    public FontPickerViewModel(MainViewModel? targetViewModel) {
        SetupNewTarget(targetViewModel);
    }

    public void SetupNewTarget(MainViewModel? targetViewModel) {
        RevertFontIfNotApplied();

        TargetViewModel = targetViewModel;
        _isApplied = false;

        if (TargetViewModel is not null)
            WeakReferenceMessenger.Default.Send(new BackupSelectionMessage(TargetViewModel));
    }

    [RelayCommand]
    private void Apply() {
        if (TargetViewModel is null)
            return;

        _isApplied = true;

        WeakReferenceMessenger.Default.Send(new ClearSelectionBackupMessage(TargetViewModel));
        System.Windows.Application.Current.Windows.OfType<Views.FontPickerWindow>().FirstOrDefault()?.Close();
    }

    public void RevertFontIfNotApplied() {
        if (!_isApplied && TargetViewModel is not null)
            WeakReferenceMessenger.Default.Send(new RestoreSelectionMessage(TargetViewModel));
    }

    partial void OnSelectedFontChanged(FontFamily? value) {
        if (TargetViewModel is null || value is null)
            return;

        WeakReferenceMessenger.Default.Send(new ChangeFontFamilyMessage(TargetViewModel, value));
    }

    partial void OnSelectedFontSizeChanged(double value) {
        if (value > 100) {
            SelectedFontSize = 100;
            return;
        }
        if (value < 8)   {
            SelectedFontSize = 8;  
            return;
        }

        if (TargetViewModel is not null)
            WeakReferenceMessenger.Default.Send(new ChangeFontSizeMessage(TargetViewModel, value));
    }
}
