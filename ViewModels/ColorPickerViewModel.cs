using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using WindowsStickies.Models;


namespace WindowsStickies.ViewModels; 

internal partial class ColorPickerViewModel : BaseTitleBarViewModel {
    private string? _originalColor;
    private bool _isApplied = false;
    
    [ObservableProperty]
    private MainViewModel? _targetViewModel;

    [ObservableProperty]
    private OpenColorPickerMessage.PickerMode _mode;

    [ObservableProperty]
    private Color _selectedColor;

    public ColorPickerViewModel(MainViewModel? targetViewModel, OpenColorPickerMessage.PickerMode mode) {
        SetupNewTarget(targetViewModel, mode);
    }

    public void SetupNewTarget(MainViewModel? targetViewModel, OpenColorPickerMessage.PickerMode mode) {
        RevertColorIfNotApplied();

        TargetViewModel = targetViewModel;
        Mode = mode;
        _isApplied = false;

        if (TargetViewModel is not null) {
            string colorString = "#FF000000";
            
            if (Mode == OpenColorPickerMessage.PickerMode.FontColor)
                colorString = TargetViewModel.SelectedFontColor;
            
            else if (Mode == OpenColorPickerMessage.PickerMode.HighlightColor)
                colorString = TargetViewModel.SelectedHighlightColor;

            else if (Mode == OpenColorPickerMessage.PickerMode.BackgroundColor) {
                _originalColor = TargetViewModel.StickyModel.BackgroundColor;
                colorString = _originalColor;
            }

            if (ColorConverter.ConvertFromString(colorString) is Color color)
                SelectedColor = color;
        }

        if (TargetViewModel is not null && Mode == OpenColorPickerMessage.PickerMode.BackgroundColor) {
            _originalColor = TargetViewModel.StickyModel.BackgroundColor;
            if (ColorConverter.ConvertFromString(_originalColor) is Color color)
                SelectedColor = color;
        }
    }

    [RelayCommand]
    private void ApplyColor() {
        if (TargetViewModel is null)
            return;

        _isApplied = true;

        if (Mode == OpenColorPickerMessage.PickerMode.FontColor)
            WeakReferenceMessenger.Default.Send(new ChangeFontColorMessage(TargetViewModel, SelectedColor));

        else if (Mode == OpenColorPickerMessage.PickerMode.HighlightColor)
            WeakReferenceMessenger.Default.Send(new ChangeHighlightColorMessage(TargetViewModel, SelectedColor));

        else if (Mode == OpenColorPickerMessage.PickerMode.BackgroundColor)
            TargetViewModel.StickyModel.BackgroundColor = SelectedColor.ToString();
    }

    partial void OnSelectedColorChanged(Color value) {
        if (TargetViewModel is not null && Mode == OpenColorPickerMessage.PickerMode.BackgroundColor)
            TargetViewModel.StickyModel.BackgroundColor = value.ToString();
    }

    public void RevertColorIfNotApplied() {
        if (!_isApplied && TargetViewModel is not null && Mode == OpenColorPickerMessage.PickerMode.BackgroundColor)
            TargetViewModel.StickyModel.BackgroundColor = _originalColor!;
    }
}
