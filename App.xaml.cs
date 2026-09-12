using System.Windows;

using CommunityToolkit.Mvvm.Messaging;

using WindowsStickies.ViewModels;
using WindowsStickies.Views;

namespace WindowsStickies; 
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
    private static System.Threading.Mutex? _mutex;
    private const string MutexName = "Stickies_SingleInstance_Mutex";

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    private const int SW_RESTORE = 9;

    protected override void OnStartup(StartupEventArgs e) {
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew) {
            IntPtr hWnd = FindWindow(null, "Stickies");
            if (hWnd != IntPtr.Zero) {
                ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
            }
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Locales.Localizer.Instance.SetLanguage(Services.SettingsService.Instance.LanguageCode);
        Services.WindowService.Instance.Initialize();
    }
}
