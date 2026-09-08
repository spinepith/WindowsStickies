using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace WindowsStickies.Views {
    public partial class BaseTitleBarView : UserControl {
        public BaseTitleBarView() {
            InitializeComponent();
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e) {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                if (TopLevel.GetTopLevel(this) is Window window)
                    window.BeginMoveDrag(e);
        }
    }
}