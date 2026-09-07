using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace WindowsStickies.Views {
    public partial class TitleBarView : UserControl {
        public TitleBarView() {
            InitializeComponent();
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e) {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) {
                var window = TopLevel.GetTopLevel(this) as Window;
                window?.BeginMoveDrag(e);
            }
        }
    }
}