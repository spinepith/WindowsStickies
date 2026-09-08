using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;


namespace WindowsStickies.Services;

public partial class SettingsService : ObservableObject {
    private static readonly string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stickies", "settings.json");

    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= Load();

    #region SETTINGS
    [ObservableProperty]
    private bool _isRoundedCorners = false;

    [ObservableProperty]
    private string _languageCode = "ru";
    #endregion

    public SettingsService() {
        PropertyChanged += (s, e) => Save();
    }

    private static SettingsService Load() {
        try {
            if (File.Exists(Path)) {
                var json = File.ReadAllText(Path);
                var settings = JsonSerializer.Deserialize<SettingsService>(json) ?? new SettingsService();
                
                settings.PropertyChanged += (s, e) => settings.Save();
                return settings;
            }
        }
        catch (Exception ex) {
            Debug.WriteLine(ex);
        }

        var newSettings = new SettingsService();
        newSettings.Save();
        return newSettings;
    }

    private void Save() {
        try {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, json);
        }
        catch (Exception ex) {
            Debug.WriteLine(ex);
        }
    }
}
