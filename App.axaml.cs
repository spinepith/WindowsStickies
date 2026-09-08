using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using WindowsStickies.ViewModels;
using WindowsStickies.Views;

using CommunityToolkit.Mvvm.Messaging;

namespace WindowsStickies;

public partial class App : Application {
    private AboutWindow? _aboutWindow;

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        Locales.Localizer.Instance.SetLanguage(Services.SettingsService.Instance.LanguageCode);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = new MainViewModel(),
            };

            WeakReferenceMessenger.Default.Register<App, Models.NewNoteMessage>(this, (r, m) => {
                var newNote = new MainWindow {
                    DataContext = new MainViewModel()
                };
                newNote.Show();
            });

            WeakReferenceMessenger.Default.Register<App, Models.OpenAboutMessage>(this, (r, m) => {
                if (r._aboutWindow is not null && r._aboutWindow.IsVisible) {
                    r._aboutWindow.Activate();
                    return;
                }
                r._aboutWindow = new AboutWindow {
                    DataContext = new BaseTitleBarViewModel()
                };
                r._aboutWindow.Show();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

}