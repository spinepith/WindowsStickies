using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.Messaging;

namespace WindowsStickies.Services;

public class HotkeyService {
    public static HotkeyService Instance { get; } = new();

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID_SHOW_ALL = 9000;
    private const int HOTKEY_ID_NEW_NOTE = 9001;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    private IntPtr _hwnd;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    public void Initialize(Window mainWindow) {
        var helper = new WindowInteropHelper(mainWindow);
        _hwnd = helper.Handle;

        if (_hwnd == IntPtr.Zero) {
            mainWindow.SourceInitialized += (s, e) => {
                _hwnd = new WindowInteropHelper(mainWindow).Handle;
                RegisterHotkeys();
                HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
            };
        } else {
            RegisterHotkeys();
            HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
        }
    }

    private void RegisterHotkeys() {
        RegisterHotKey(_hwnd, HOTKEY_ID_SHOW_ALL, MOD_CONTROL | MOD_SHIFT | MOD_ALT, 0x53); // S key
        RegisterHotKey(_hwnd, HOTKEY_ID_NEW_NOTE, MOD_CONTROL | MOD_SHIFT | MOD_ALT, 0x4E); // N key
    }

    public void Unregister() {
        if (_hwnd != IntPtr.Zero) {
            UnregisterHotKey(_hwnd, HOTKEY_ID_SHOW_ALL);
            UnregisterHotKey(_hwnd, HOTKEY_ID_NEW_NOTE);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
        if (msg == WM_HOTKEY) {
            int id = wParam.ToInt32();

            if (id == HOTKEY_ID_SHOW_ALL) {
                ShowAllStickies();
                handled = true;
            } else if (id == HOTKEY_ID_NEW_NOTE) {
                CreateNewNote();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private void ShowAllStickies() {
        Application.Current.Dispatcher.Invoke(() => {
            foreach (Window window in Application.Current.Windows) {
                if (window is Views.MainWindow) {
                    var hwnd = new WindowInteropHelper(window).Handle;

                    if (IsIconic(hwnd))
                        ShowWindow(hwnd, SW_RESTORE);
                    else
                        ShowWindow(hwnd, SW_SHOW);

                    window.Topmost = true;
                    window.Topmost = false;

                    SetForegroundWindow(hwnd);
                }
            }
        });
    }

    private void CreateNewNote() {
        Application.Current.Dispatcher.Invoke(() => {
            WeakReferenceMessenger.Default.Send(new Models.NewStickyMessage());

            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                var newWindow = Application.Current.Windows.OfType<Views.MainWindow>().LastOrDefault();
                if (newWindow != null) {
                    newWindow.Topmost = true;
                    newWindow.Topmost = false;
                    newWindow.Activate();

                    if (newWindow.Content is System.Windows.Controls.Grid grid) {
                        var textBox = FindTextBox(grid);
                        textBox?.Focus();
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        });
    }

    private System.Windows.Controls.RichTextBox? FindTextBox(System.Windows.DependencyObject parent) {
        int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++) {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is System.Windows.Controls.RichTextBox rtb)
                return rtb;

            var result = FindTextBox(child);
            if (result != null)
                return result;
        }
        return null;
    }
}
