using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WindowsStickies.Views {
    /// <summary>
    /// Логика взаимодействия для ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window {
        public ColorPickerWindow() {
            InitializeComponent();

            Closing += (s, e) => {
                if (DataContext is ViewModels.ColorPickerViewModel vm)
                    vm.RevertColorIfNotApplied();
            };
        }
    }
}
