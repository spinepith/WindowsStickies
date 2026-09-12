using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace WindowsStickies.Views;

public partial class MainWindow : Window {
    private readonly DispatcherTimer _saveTimer;
    private bool _isLoaded = false;

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_DONOTROUND = 1;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public MainWindow() {
        InitializeComponent();

        _saveTimer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _saveTimer.Tick += (s, e) => {
            _saveTimer.Stop();
            Services.SessionService.Instance.Save();
        };

        Services.SettingsService.Instance.PropertyChanged += (s, e) => {
            if (e.PropertyName is nameof(Services.SettingsService.IsRoundedCorners))
                ApplyCornerPreference();
        };

        SourceInitialized += (s, e) => {
            ApplyCornerPreference();

            if (DataContext is ViewModels.MainViewModel vm) {
                Left = vm.StickyModel.X;
                Top = vm.StickyModel.Y;
                Width = vm.StickyModel.Width;
                Height = vm.StickyModel.Height;
                WindowState = vm.StickyModel.WindowState;
            }

            UpdateMaximizedMargin();
            _isLoaded = true;
        };

        StateChanged += (s, e) => {
            UpdateMaximizedMargin();

            if (_isLoaded && DataContext is ViewModels.MainViewModel vm) {
                vm.StickyModel.WindowState = WindowState;
                _saveTimer.Stop();
                _saveTimer.Start();
            }
        };

        LocationChanged += (s, e) => {
            if (!_isLoaded)
                return;

            if (DataContext is ViewModels.MainViewModel vm) {
                vm.StickyModel.X = Left;
                vm.StickyModel.Y = Top;
                _saveTimer.Stop();
                _saveTimer.Start();
            }
        };

        SizeChanged += (s, e) => {
            if (!_isLoaded)
                return;

            if (DataContext is ViewModels.MainViewModel vm) {
                vm.StickyModel.Width = Width;
                vm.StickyModel.Height = Height;
                _saveTimer.Stop();
                _saveTimer.Start();
            }
        };

        Closed += (s, e) => {
            if (DataContext is ViewModels.MainViewModel vm) {
                Services.SessionService.Instance.ActiveStickies.Remove(vm.StickyModel);
                Services.SessionService.Instance.Save(true);
            }
        };
    }

    private void UpdateMaximizedMargin() {
        if (Content is System.Windows.Controls.Grid grid)
            grid.Margin = WindowState == WindowState.Maximized ? new Thickness(8) : new Thickness(0);
    }

    private void ApplyCornerPreference() {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero) {
            var cornerPreference = Services.SettingsService.Instance.IsRoundedCorners ? DWMWCP_ROUND : DWMWCP_DONOTROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
        }
    }
}