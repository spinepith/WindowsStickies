using System.Security.Policy;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using WindowsStickies.Models;

namespace WindowsStickies.ViewModels; 

public partial class HyperlinkViewModel : BaseTitleBarViewModel {
    
    [ObservableProperty]
    private MainViewModel? _targetViewModel;

    [ObservableProperty]
    private string _url = "";

    [ObservableProperty]
    private string _displayText = "";

    [ObservableProperty]
    private bool _isEditingExistingLink;

    public HyperlinkViewModel(MainViewModel? targetViewModel) {
        SetupNewTarget(targetViewModel);
    }

    public void SetupNewTarget(MainViewModel? targetViewModel) {
        TargetViewModel = targetViewModel;

        if (TargetViewModel is not null) {
            DisplayText = TargetViewModel.SelectedText ?? "";

            string detectedUrl = TargetViewModel.SelectedHyperlinkUrl;

            if (!string.IsNullOrEmpty(detectedUrl)) {
                Url = detectedUrl;
                IsEditingExistingLink = true;
            }
            else {
                Url = "";
                IsEditingExistingLink = false;
            }
        }
    }

    [RelayCommand]
    private void Apply() {
        if (TargetViewModel is null || string.IsNullOrWhiteSpace(Url))
            return;
        WeakReferenceMessenger.Default.Send(new HyperlinkMessage(TargetViewModel, HyperlinkMessage.HyperlinkAction.Apply, Url, DisplayText));
        System.Windows.Application.Current.Windows.OfType<Views.HyperlinkWindow>().FirstOrDefault()?.Close();
    }

    [RelayCommand]
    private void Remove() {
        if (TargetViewModel is null || !IsEditingExistingLink)
            return;
        WeakReferenceMessenger.Default.Send(new HyperlinkMessage(TargetViewModel, HyperlinkMessage.HyperlinkAction.Remove));
        System.Windows.Application.Current.Windows.OfType<Views.HyperlinkWindow>().FirstOrDefault()?.Close();
    }
}
