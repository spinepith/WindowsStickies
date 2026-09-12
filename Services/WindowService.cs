using System.Windows;

using CommunityToolkit.Mvvm.Messaging;

using WindowsStickies.ViewModels;
using WindowsStickies.Views;


namespace WindowsStickies.Services;

public class WindowService {
    public static WindowService Instance { get; } = new();

    private AboutWindow? _aboutWindow;
    private FindTextWindow? _findTextWindow;

    public void Initialize() {
        WeakReferenceMessenger.Default.Register<WindowService, Models.NewStickyMessage>(this, (r, m) => {
            double targetX = 100;
            double targetY = 100;

            var stickies = SessionService.Instance.ActiveStickies;
            while (System.Linq.Enumerable.Any(stickies, s => Math.Abs(s.X - targetX) < 5 && Math.Abs(s.Y - targetY) < 5)) {
                targetX += 30;
                targetY += 30;
            }

            var newSticky = new Models.StickyModel {
                X = targetX,
                Y = targetY
            };

            SessionService.Instance.ActiveStickies.Add(newSticky);
            var newNote = new MainWindow {
                DataContext = new MainViewModel(newSticky)
            };
            newNote.Show();
        });

        WeakReferenceMessenger.Default.Register<WindowService, Models.OpenFindTextMessage>(this, (r, m) => {
            if (r._findTextWindow is not null && r._findTextWindow.IsVisible) {
                if (r._findTextWindow.WindowState == WindowState.Minimized)
                    r._findTextWindow.WindowState = WindowState.Normal;

                if (r._findTextWindow.DataContext is FindTextViewModel vm)
                    vm.TargetViewModel = m.SourceViewModel;

                var newOwner = Enumerable.FirstOrDefault(Application.Current.Windows.OfType<MainWindow>(), w => w.DataContext == m.SourceViewModel);

                if (newOwner is not null) {
                    r._findTextWindow.Owner = newOwner;

                    r._findTextWindow.Left = newOwner.Left + (newOwner.ActualWidth - r._findTextWindow.ActualWidth) / 2;
                    r._findTextWindow.Top = newOwner.Top + (newOwner.ActualHeight - r._findTextWindow.ActualHeight) / 2;
                }

                r._findTextWindow.Activate();
                return;
            }

            var owner = Enumerable.FirstOrDefault(Application.Current.Windows.OfType<MainWindow>(), w => w.DataContext == m.SourceViewModel);

            r._findTextWindow = new FindTextWindow {
                Owner = owner,
                DataContext = new FindTextViewModel(m.SourceViewModel)
            };

            r._findTextWindow.Show();
        });

        WeakReferenceMessenger.Default.Register<WindowService, Models.OpenAboutMessage>(this, (r, m) => {
            if (r._aboutWindow is not null && r._aboutWindow.IsVisible) {
                if (r._aboutWindow.WindowState == WindowState.Minimized)
                    r._aboutWindow.WindowState = WindowState.Normal;
                r._aboutWindow.Activate();
                return;
            }
            r._aboutWindow = new AboutWindow {
                DataContext = new BaseTitleBarViewModel()
            };
            r._aboutWindow.Show();
        });

        OpenSavedStickies();
    }

    private void OpenSavedStickies() {
        foreach (var sticky in SessionService.Instance.ActiveStickies) {
            var window = new MainWindow {
                DataContext = new MainViewModel(sticky)
            };
            window.Show();
        }
    }
}
