using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WindowsStickies.Views {
    /// <summary>
    /// Логика взаимодействия для AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x00040000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public AboutWindow() {
            InitializeComponent();

            SourceInitialized += (s, e) => {
                var hwnd = new WindowInteropHelper(this).Handle;
                int style = GetWindowLong(hwnd, GWL_STYLE);
                SetWindowLong(hwnd, GWL_STYLE, style | WS_THICKFRAME);
                ApplyCornerPreference();
            };

            Services.SettingsService.Instance.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(Services.SettingsService.IsRoundedCorners))
                    ApplyCornerPreference();
            };
        }

        private void ApplyCornerPreference() {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero) {
                var cornerPreference = Services.SettingsService.Instance.IsRoundedCorners ? DWMWCP_ROUND : DWMWCP_DONOTROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
            }
        }
    }
}
