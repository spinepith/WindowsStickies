using Avalonia.Controls;
using System.Runtime.InteropServices;
using System;

namespace WindowsStickies.Views;

public partial class MainWindow : Window {
    private readonly Avalonia.Threading.DispatcherTimer _saveTimer;
    private bool _isLoaded = false;

    public MainWindow() {
        InitializeComponent();

        _saveTimer = new Avalonia.Threading.DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _saveTimer.Tick += (s, e) => {
            _saveTimer.Stop();
            Services.SessionService.Instance.Save();
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            this.Opened += (s, e) => {
                if (DataContext is ViewModels.MainViewModel vm) {
                    Position = new Avalonia.PixelPoint((int)vm.StickyModel.X, (int)vm.StickyModel.Y);
                    Width = vm.StickyModel.Width;
                    Height = vm.StickyModel.Height;
                }
                
                ApplyCornerPreference();
                
                _isLoaded = true; 
            };
            
            Services.SettingsService.Instance.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(Services.SettingsService.IsRoundedCorners)) {
                    ApplyCornerPreference();
                }
            };
        }

        PositionChanged += (s, e) => {
            if (!_isLoaded)
                return;

            if (DataContext is ViewModels.MainViewModel vm) {
                vm.StickyModel.X = Position.X;
                vm.StickyModel.Y = Position.Y;
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

    private void ApplyCornerPreference() {
        if (TryGetPlatformHandle()?.Handle is IntPtr hwnd) {
            var preference = Services.SettingsService.Instance.IsRoundedCorners ? DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND : DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND;
            SetWindowCornerPreference(hwnd, preference);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    private enum DWM_WINDOW_CORNER_PREFERENCE {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    private static void SetWindowCornerPreference(IntPtr hwnd, DWM_WINDOW_CORNER_PREFERENCE preference) {
        int pref = (int)preference;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
    }
}