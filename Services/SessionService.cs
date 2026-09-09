using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

using CommunityToolkit.Mvvm.ComponentModel;

using WindowsStickies.Models;


namespace WindowsStickies.Services;

public partial class SessionService : ObservableObject {
    private static readonly string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stickies", "session.json");

    private static SessionService? _instance;
    public static SessionService Instance => _instance ??= Load();

    #region SESSION DATA
    [ObservableProperty]
    private ObservableCollection<StickyModel> _activeStickies = new();
    #endregion

    private Avalonia.Threading.DispatcherTimer _saveTimer;
    private bool _isSavePending;

    public SessionService() {
        _saveTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _saveTimer.Tick += (s, e) => {
            _saveTimer.Stop();
            if (_isSavePending) {
                Save(true);
            }
        };

        PropertyChanged += (s, e) => Save();
        ActiveStickies.CollectionChanged += (s, e) => {
            if (e.NewItems is not null)
                foreach (StickyModel item in e.NewItems)
                    item.PropertyChanged += (sender, args) => Save();

            if (e.OldItems is not null)
                foreach (StickyModel item in e.OldItems)
                    item.PropertyChanged -= (sender, args) => Save();

            Save();
        };
    }

    private static SessionService Load() {
        try {
            if (File.Exists(Path)) {
                var json = File.ReadAllText(Path);
                var session = JsonSerializer.Deserialize<SessionService>(json) ?? new SessionService();
                
                session.PropertyChanged += (s, e) => session.Save();
                session.ActiveStickies.CollectionChanged += (s, e) => session.Save();
                
                if (session.ActiveStickies.Count == 0) {
                    var defaultSticky = new StickyModel();
                    defaultSticky.PropertyChanged += (sender, args) => session.Save();
                    session.ActiveStickies.Add(defaultSticky);
                }
                else
                    foreach (var sticky in session.ActiveStickies)
                        sticky.PropertyChanged += (sender, args) => session.Save();
                
                return session;
            }
        }
        catch (Exception ex) {
            Debug.WriteLine(ex);
        }

        var newSession = new SessionService();
        var newSticky = new StickyModel();

        newSticky.PropertyChanged += (sender, args) => newSession.Save();
        newSession.ActiveStickies.Add(newSticky);
        newSession.Save();
        
        return newSession;
    }

    public void Save(bool force = false) {
        if (force) {
            _saveTimer?.Stop();
            _isSavePending = false;

            try {
                var copy = System.Linq.Enumerable.ToList(ActiveStickies);
                var dataToSave = new { ActiveStickies = copy };
                var json = JsonSerializer.Serialize(dataToSave, new JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                File.WriteAllText(Path, json);
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
            }
        }
        else {
            _isSavePending = true;
            if (_saveTimer is not null && !_saveTimer.IsEnabled)
                _saveTimer.Start();
        }
    }
}
