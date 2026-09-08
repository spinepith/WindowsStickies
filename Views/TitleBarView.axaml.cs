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
                if (TopLevel.GetTopLevel(this) is Window window) {
                    if (e.ClickCount is 2)
                        window.WindowState = window.WindowState == WindowState.Maximized  ? WindowState.Normal : WindowState.Maximized;
                    else
                        window.BeginMoveDrag(e);
                }
            }
        }
    }
}