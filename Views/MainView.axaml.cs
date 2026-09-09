using System;
using System.Linq;

using Avalonia.Controls;
using Avalonia.VisualTree;

using WindowsStickies.ViewModels;

namespace WindowsStickies.Views {
    public partial class MainView : ContentPage {
        private double _currentFontSize = 16.0;

        public MainView() {
            InitializeComponent();
            Loaded += MainView_Loaded;
        }

        private void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            UpdateContextMenuLocale();
            StyleHyperlinkPopup();

            // БАГ: Avalonia с исчезновением текста при ПЕРВОМ открытии попапа гиперссылки.
            // Происходит из-за того, что при первом открытии координаты попапа еще не просчитаны, 
            // и фокус внутри него сводит с ума главный ScrollViewer.
            // РЕШЕНИЕ: невидимо "промигиваем" попап при старте окна в том же кадре, заставляя движок его инициализировать.
            var popup = Editor.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().FirstOrDefault(p => p.Name is "HyperlinkPopup");
            if (popup is not null) {
                popup.IsOpen = true;
                popup.IsOpen = false;
            }

            Locales.Localizer.Instance.PropertyChanged += (s, ev) => {
                if (ev.PropertyName is "Item") {
                    UpdateContextMenuLocale();
                    UpdateHyperlinkPopupLocale();
                }
            };

            /* ЛОГИКА ОТОБРАЖЕНИЯ КОНТЕКСТНОГО МЕНЮ */
            var docIC = Editor.GetVisualDescendants().OfType<ItemsControl>().FirstOrDefault(c => c.Name is "DocIC");
            if (docIC is not null && docIC.ContextMenu is not null) {
                var items = docIC.ContextMenu.Items.OfType<MenuItem>().ToList();
                
                // Подписываемся на клик по кнопке "Удалить гиперссылку"
                if (items.Count >= 8) {
                    items[7].Click += (senderClick, evClick) => {
                        WiggleCursorToFixCache();
                    };
                }

                docIC.ContextMenu.Opening += (sender, ev) => {
                    var currentItems = docIC.ContextMenu.Items.OfType<MenuItem>().ToList();

                    // Скрываем неактивные кнопки (Копировать, Вырезать, Удалить)
                    bool hasSelection = Editor.FlowDocument.Selection.Length > 0;
                    currentItems[0].IsVisible = hasSelection;
                    currentItems[3].IsVisible = hasSelection;
                    currentItems[4].IsVisible = hasSelection;

                    bool hasHyperlink = IsCursorOnHyperlink();

                    if (currentItems.Count >= 8) {
                        currentItems[5].IsVisible = !hasHyperlink;
                        currentItems[6].IsVisible = hasHyperlink;
                        currentItems[7].IsVisible = hasHyperlink;
                    }
                };
            }

            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel is not null) {
                topLevel.Deactivated += (s, ev) => Editor.IsCaretVisible = false;
                topLevel.Activated += (s, ev) => Editor.IsCaretVisible = true;
            }

            if (DataContext is MainViewModel vm) {
                if (string.IsNullOrWhiteSpace(vm.StickyModel.Text)) {
                    Editor.NewDocument();
                    SaveTextToModel();
                }
                else
                    Editor.LoadXamlString(vm.StickyModel.Text);

                Editor.Zoom = vm.StickyModel.Zoom;
                vm.StickyModel.PropertyChanged += (s, ev) => {
                    if (ev.PropertyName is nameof(vm.StickyModel.Zoom))
                        Editor.Zoom = vm.StickyModel.Zoom;
                };

                Editor.KeyUp += (s, ev) => {
                    SaveTextToModel();
                    if (ev.Key == Avalonia.Input.Key.Left || ev.Key == Avalonia.Input.Key.Right || ev.Key == Avalonia.Input.Key.Up || ev.Key == Avalonia.Input.Key.Down) {
                        var fmt = Editor.FlowDocument.Selection.GetFormatting(Avalonia.Controls.Documents.Run.FontSizeProperty);
                        _currentFontSize = fmt is null ? 16.0 : Convert.ToDouble(fmt);
                    }
                };

                Editor.PointerReleased += (s, ev) => {
                    SaveTextToModel();
                    var fmt = Editor.FlowDocument.Selection.GetFormatting(Avalonia.Controls.Documents.Run.FontSizeProperty);
                    _currentFontSize = fmt is null ? 16.0 : Convert.ToDouble(fmt);
                };

                Editor.LostFocus += (s, ev) => SaveTextToModel();

                vm.TitleBar.EditorAction = (action) => {
                    switch (action) {
                        case "DeleteText":
                            Editor.FlowDocument.Selection.Text = "";
                            break;

                        case "IncreaseFontSize": {
                                var start = Editor.FlowDocument.Selection.Start;
                                var end = Editor.FlowDocument.Selection.End;

                                if (start != end) {
                                    Editor.FlowDocument.Select(Math.Min(start, end), Math.Abs(end - start));

                                    _currentFontSize += 1.0;
                                    Editor.FlowDocument.Selection.ApplyFormatting(Avalonia.Controls.Documents.Run.FontSizeProperty, _currentFontSize);

                                    Editor.FlowDocument.Select(Math.Min(start, end), Math.Abs(end - start));
                                    Editor.Focus();
                                }
                                break;
                            }

                        case "DecreaseFontSize": {
                                var start = Editor.FlowDocument.Selection.Start;
                                var end = Editor.FlowDocument.Selection.End;

                                if (start != end) {
                                    Editor.FlowDocument.Select(Math.Min(start, end), Math.Abs(end - start));

                                    if (_currentFontSize > 1.0)
                                        _currentFontSize -= 1.0;
                                    Editor.FlowDocument.Selection.ApplyFormatting(Avalonia.Controls.Documents.Run.FontSizeProperty, _currentFontSize);

                                    Editor.FlowDocument.Select(Math.Min(start, end), Math.Abs(end - start));
                                    Editor.Focus();
                                }
                                break;
                            }

                        case "StrikethroughFont":
                            Editor.FlowDocument.Selection.ApplyFormatting(Avalonia.Controls.Documents.Inline.TextDecorationsProperty, Avalonia.Media.TextDecorationLocation.Strikethrough);
                            break;
                    }
                    SaveTextToModel();
                };
            }
        }

        private void SaveTextToModel() {
            if (DataContext is MainViewModel vm)
                vm.StickyModel.Text = Editor.SaveXamlString();
        }

        private void UpdateContextMenuLocale() {
            var docIC = Editor.GetVisualDescendants().OfType<ItemsControl>().FirstOrDefault(c => c.Name is "DocIC");
            if (docIC is not null && docIC.ContextMenu is not null) {
                var items = docIC.ContextMenu.Items.OfType<MenuItem>().ToList();
                if (items.Count >= 8) {
                    var localizer = Locales.Localizer.Instance;

                    items[0].Header = localizer["ContextMenu_Copy"];
                    items[1].Header = localizer["ContextMenu_Paste"];
                    items[2].Header = localizer["ContextMenu_PasteWithoutFormatting"];
                    items[3].Header = localizer["ContextMenu_Cut"];
                    items[4].Header = localizer["ContextMenu_Delete"];
                    items[5].Header = localizer["ContextMenu_InsertHyperlink"];
                    items[6].Header = localizer["ContextMenu_EditHyperlink"];
                    items[7].Header = localizer["ContextMenu_RemoveHyperlink"];
                }
            }
        }

        private void UpdateHyperlinkPopupLocale() {
            var popup = Editor.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().FirstOrDefault(p => p.Name is "HyperlinkPopup");
            if (popup?.Child is Border popupBorder) {
                var localizer = Locales.Localizer.Instance;

                foreach (var textBox in popupBorder.GetVisualDescendants().OfType<TextBox>()) {
                    if (textBox.Name is "HyperlinkTextBox")
                        textBox.PlaceholderText = localizer["HyperlinkMenu_Placeholder"];
                    if (textBox.Name is "HyperlinkUrlBox")
                        textBox.PlaceholderText = "URL";
                }

                foreach (var button in popupBorder.GetVisualDescendants().OfType<Button>()) {
                    if (button.Name is "HyperlinkDeleteButton")
                        button.Content = localizer["HyperlinkMenu_Remove"];
                    if (button.Name is "HyperlinkCancelButton")
                        button.Content = localizer["HyperlinkMenu_Cancel"];
                }
            }
        }

        private void StyleHyperlinkPopup() {
            var popup = Editor.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().FirstOrDefault(p => p.Name is "HyperlinkPopup");
            if (popup is not null) {
                if (popup.Child is Border popupBorder) {
                    popupBorder.Background = Avalonia.Media.Brush.Parse("#FFFFFF");
                    popupBorder.BorderBrush = Avalonia.Media.Brush.Parse("#000000");

                    popupBorder.CornerRadius = new Avalonia.CornerRadius(8);
                    popupBorder.BorderThickness = new Avalonia.Thickness(1);
                    popupBorder.Padding = new Avalonia.Thickness(12);

                    popup.Opened += (s, ev) => {
                        UpdateHyperlinkPopupLocale();

                        var titleBlock = popupBorder.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name is "HyperlinkPopupTitle");
                        if (titleBlock is not null) {
                            titleBlock.IsVisible = false;
                            titleBlock.Height = 0;
                        }

                        foreach (var textBlock in popupBorder.GetVisualDescendants().OfType<TextBlock>())
                            textBlock.Foreground = Avalonia.Media.Brushes.Black;

                        var externalTextBlocks = popupBorder.GetVisualDescendants().OfType<TextBlock>().Where(tb => {
                            var parent = tb.Parent;
                            while (parent is not null) {
                                if (parent is TextBox)
                                    return false;
                                parent = parent.Parent;
                            }
                            return true;
                        });

                        foreach (var tb in externalTextBlocks) {
                            if (tb.Text is "Text" || tb.Text is "URL") {
                                tb.IsVisible = false;
                                tb.Height = 0;
                            }
                        }

                        foreach (var textBox in popupBorder.GetVisualDescendants().OfType<TextBox>()) {
                            textBox.Foreground = Avalonia.Media.Brushes.Black;
                            textBox.Background = Avalonia.Media.Brushes.White;
                            textBox.CaretBrush = Avalonia.Media.Brushes.Black;
                            textBox.BorderBrush = Avalonia.Media.Brushes.Black;
                            textBox.BorderThickness = new Avalonia.Thickness(1);

                            textBox.ApplyTemplate();

                            var innerBorder = textBox.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name is "PART_BorderElement");
                            if (innerBorder is not null) {
                                innerBorder.Background = Avalonia.Media.Brushes.White;
                                innerBorder.BorderBrush = Avalonia.Media.Brushes.Black;
                                innerBorder.BorderThickness = new Avalonia.Thickness(1);
                            }
                        }

                        foreach (var button in popupBorder.GetVisualDescendants().OfType<Button>()) {
                            button.Foreground = Avalonia.Media.Brushes.Black;

                            if (button.Name is "HyperlinkDeleteButton") {
                                button.Click -= PopupRemove_Click;
                                button.Click += PopupRemove_Click;
                            }

                            button.ApplyTemplate();
                            var presenter = button.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().FirstOrDefault();

                            if (presenter is not null) {
                                presenter.Transitions?.Clear();
                                presenter.Transitions = null;

                                var baseBrush = Avalonia.Media.Brush.Parse("#FFFFFF");
                                var hoverBrush = Avalonia.Media.Brush.Parse("#EEEEEE");

                                presenter.Background = baseBrush;

                                button.PointerEntered += (sender, e) => presenter.Background = hoverBrush;
                                button.PointerExited += (sender, e) => presenter.Background = baseBrush;

                                presenter.BorderBrush = Avalonia.Media.Brushes.Black;
                                presenter.BorderThickness = new Avalonia.Thickness(1);
                            }
                        }
                    };
                }
            }
        }

        private void PopupRemove_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            if (sender is Button btn)
                btn.IsVisible = false;
            WiggleCursorToFixCache();
        }

        private void WiggleCursorToFixCache() {
            // Запускаем асинхронно, чтобы библиотека успела закончить удаление ссылки
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                try {
                    var pos = Editor.FlowDocument.Selection.Start;
                    var len = Editor.FlowDocument.Selection.Length;
                    
                    // Имитируем микро-движение курсора, чтобы заставить библиотеку пересчитать кэш ссылок
                    if (pos > 0)
                        Editor.FlowDocument.Select(pos - 1, 0);
                    else Editor.FlowDocument.Select(pos + 1, 0);
                    
                    Editor.FlowDocument.Select(pos, len);
                }
                catch { }
            });
        }

        private bool IsCursorOnHyperlink() {
            var method = Editor.FlowDocument.GetType().GetMethod(
                "GetHyperlinkAtSelection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            
            if (method is not null) {
                var hyperlink = method.Invoke(Editor.FlowDocument, null);
                return hyperlink is not null;
            }
            
            return false;
        }
    }
}
