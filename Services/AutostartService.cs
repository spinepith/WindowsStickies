using System;
using System.Diagnostics;

using Microsoft.Win32;


namespace WindowsStickies.Services;

public static class AutostartService {
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Stickies";

    private static string ExePath =>
        Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;

    public static void Set(bool enabled) {
        try {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (enabled)
                key?.SetValue(AppName, ExePath);
            else
                key?.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch (Exception ex) {
            Debug.WriteLine(ex);
        }
    }
}
