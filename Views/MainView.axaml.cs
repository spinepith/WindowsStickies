using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using WindowsStickies.ViewModels;

namespace WindowsStickies.Views {
    public partial class MainView : ContentPage {
        public MainView() {
            InitializeComponent();
            Loaded += MainView_Loaded;
        }

        private void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            if (DataContext is MainViewModel vm) {
                if (string.IsNullOrWhiteSpace(vm.StickyModel.Text))
                    Editor.NewDocument();
                else
                    Editor.LoadXamlString(vm.StickyModel.Text);

                Editor.KeyUp += (s, ev) => SaveTextToModel();
                Editor.PointerReleased += (s, ev) => SaveTextToModel();
                Editor.LostFocus += (s, ev) => SaveTextToModel();
            }
        }

        private void SaveTextToModel() {
            if (DataContext is MainViewModel vm)
                vm.StickyModel.Text = Editor.SaveXamlString();
        }
    }
}