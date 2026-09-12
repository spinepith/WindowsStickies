using System.ComponentModel;
using System.Windows;

namespace WindowsStickies.Styles {
    public class CornerRadiusHelper : INotifyPropertyChanged {
        private static CornerRadiusHelper? _instance;
        public static CornerRadiusHelper Instance => _instance ??= new CornerRadiusHelper();

        public event PropertyChangedEventHandler? PropertyChanged;

        private CornerRadiusHelper() {
            Services.SettingsService.Instance.PropertyChanged += (s, e) => {
                if (e.PropertyName is nameof(Services.SettingsService.IsRoundedCorners)) {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MenuItemRadius)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PopupRadius)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonRadius)));
                }
            };
        }

        public CornerRadius MenuItemRadius => Services.SettingsService.Instance.IsRoundedCorners ? new CornerRadius(4) : new CornerRadius(0);
        public CornerRadius PopupRadius    => Services.SettingsService.Instance.IsRoundedCorners ? new CornerRadius(8) : new CornerRadius(0);
        public CornerRadius ButtonRadius   => Services.SettingsService.Instance.IsRoundedCorners ? new CornerRadius(8) : new CornerRadius(0);
    }
}
