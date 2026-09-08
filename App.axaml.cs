using System;

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
            
            foreach (var sticky in Services.SessionService.Instance.ActiveStickies) {
                var window = new MainWindow {
                    DataContext = new MainViewModel(sticky)
                };
                window.Show();
            }

            WeakReferenceMessenger.Default.Register<App, Models.NewStickyMessage>(this, (r, m) => {
                double targetX = 100;
                double targetY = 100;
                
                var stickies = Services.SessionService.Instance.ActiveStickies;
                while (System.Linq.Enumerable.Any(stickies, s => Math.Abs(s.X - targetX) < 5 && Math.Abs(s.Y - targetY) < 5)) {
                    targetX += 30;
                    targetY += 30;
                }

                var newSticky = new Models.StickyModel {
                    X = targetX,
                    Y = targetY
                };
                
                Services.SessionService.Instance.ActiveStickies.Add(newSticky);
                var newNote = new MainWindow {
                    DataContext = new MainViewModel(newSticky)
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