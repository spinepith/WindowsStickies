using Avalonia.Controls;
using System.Runtime.InteropServices;
using System;

namespace WindowsStickies.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Opened += (s, e) => {
                ApplyCornerPreference();
            };
            
            Services.SettingsService.Instance.PropertyChanged += (s, e) => {
                if (e.PropertyName is nameof(Services.SettingsService.IsRoundedCorners)) {
                    ApplyCornerPreference();
                }
            };
        }
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