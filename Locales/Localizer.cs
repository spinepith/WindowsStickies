using System.ComponentModel;
using System.Globalization;

namespace WindowsStickies.Locales;

public class Localizer : INotifyPropertyChanged {
    public static Localizer Instance  { get; } = new Localizer();
    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => Lang.ResourceManager.GetString(key, CurrentCulture) ?? key;
    
    public void SetLanguage(string languageCode) {
        CurrentCulture = new CultureInfo(languageCode);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
