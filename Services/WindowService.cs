using System.Windows;

using CommunityToolkit.Mvvm.Messaging;

using WindowsStickies.ViewModels;
using WindowsStickies.Views;


namespace WindowsStickies.Services;

public class WindowService {
    public static WindowService Instance { get; } = new();
    private FindTextWindow? _findTextWindow;
    private Views.HyperlinkWindow? _hyperlinkWindow;
    private ColorPickerWindow? _colorPickerWindow;

    private AboutWindow? _aboutWindow;

    public void Initialize() {
        WeakReferenceMessenger.Default.Register<WindowService, Models.NewStickyMessage>(this, (r, m) => {
            double targetX = 100;
            double targetY = 100;

            var stickies = SessionService.Instance.ActiveStickies;
            while (Enumerable.Any(stickies, s => Math.Abs(s.X - targetX) < 5 && Math.Abs(s.Y - targetY) < 5)) {
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
            var owner = Enumerable.FirstOrDefault(Application.Current.Windows.OfType<MainWindow>(), w => w.DataContext == m.SourceViewModel);

            if (r._findTextWindow is not null && r._findTextWindow.IsVisible) {
                if (r._findTextWindow.WindowState == WindowState.Minimized)
                    r._findTextWindow.WindowState = WindowState.Normal;

                if (r._findTextWindow.DataContext is FindTextViewModel vm) {
                    vm.TargetViewModel = m.SourceViewModel;
                    vm.SearchText = m.SourceViewModel?.SelectedText ?? "";
                }

                if (owner is not null) {
                    r._findTextWindow.Owner = owner;

                    r._findTextWindow.Left = owner.Left + (owner.ActualWidth - r._findTextWindow.ActualWidth) / 2;
                    r._findTextWindow.Top = owner.Top + (owner.ActualHeight - r._findTextWindow.ActualHeight) / 2;
                }

                r._findTextWindow.Activate();
                return;
            }

            var findVm = new FindTextViewModel(m.SourceViewModel);
            if (!string.IsNullOrEmpty(m.SourceViewModel?.SelectedText))
                findVm.SearchText = m.SourceViewModel.SelectedText;

            r._findTextWindow = new FindTextWindow {
                Owner = owner,
                DataContext = findVm
            };

            r._findTextWindow.Show();
        });

        WeakReferenceMessenger.Default.Register<WindowService, Models.OpenHyperlinkMessage>(this, (r, m) => {
              var owner = Enumerable.FirstOrDefault(System.Windows.Application.Current.Windows.OfType<MainWindow>(), w => w.DataContext == m.SourceViewModel);

              if (r._hyperlinkWindow is not null && r._hyperlinkWindow.IsVisible) {
                  if (r._hyperlinkWindow.WindowState == WindowState.Minimized)
                      r._hyperlinkWindow.WindowState = WindowState.Normal;

                  if (r._hyperlinkWindow.DataContext is HyperlinkViewModel vm)
                      vm.SetupNewTarget(m.SourceViewModel);

                  if (owner is not null) {
                      r._hyperlinkWindow.Owner = owner;
                      r._hyperlinkWindow.Left = owner.Left + (owner.Width - r._hyperlinkWindow.Width) / 2;
                      r._hyperlinkWindow.Top = owner.Top + (owner.Height - r._hyperlinkWindow.Height) / 2;
                  }

                  r._hyperlinkWindow.Activate();
                  return;
              }

              r._hyperlinkWindow = new HyperlinkWindow {
                  Owner = owner,
                  DataContext = new HyperlinkViewModel(m.SourceViewModel)
              };

              r._hyperlinkWindow.Show();
          });

        WeakReferenceMessenger.Default.Register<WindowService, Models.OpenColorPickerMessage>(this, (r, m) => {
            var owner = Enumerable.FirstOrDefault(Application.Current.Windows.OfType<MainWindow>(), w => w.DataContext == m.SourceViewModel);

            if (r._colorPickerWindow is not null && r._colorPickerWindow.IsVisible) {
                if (r._colorPickerWindow.WindowState == WindowState.Minimized)
                    r._colorPickerWindow.WindowState = WindowState.Normal;

                if (r._colorPickerWindow.DataContext is ColorPickerViewModel vm)
                    vm.SetupNewTarget(m.SourceViewModel, m.Mode);


                if (owner is not null) {
                    r._colorPickerWindow.Owner = owner;

                    r._colorPickerWindow.Left = owner.Left + (owner.ActualWidth - r._colorPickerWindow.ActualWidth) / 2;
                    r._colorPickerWindow.Top = owner.Top + (owner.ActualHeight - r._colorPickerWindow.ActualHeight) / 2;
                }

                r._colorPickerWindow.Activate();
                return;
            }

            r._colorPickerWindow = new ColorPickerWindow {
                Owner = owner,
                DataContext = new ColorPickerViewModel(m.SourceViewModel, m.Mode)
            };

            r._colorPickerWindow.Show();
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
