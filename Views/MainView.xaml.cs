using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using CommunityToolkit.Mvvm.Messaging;

using WindowsStickies.Models;


namespace WindowsStickies.Views {
    public partial class MainView : UserControl {
        private const string RtfMarkerText = "\u00B7";

        private System.Windows.Threading.DispatcherTimer _saveTimer;
        private bool _isTextDirty = false;
        private bool _isLoading = false;

        private System.IO.MemoryStream? _colorBackupStream;
        private TextPointer? _backupStart;
        private TextPointer? _backupEnd;

        public MainView() {
            InitializeComponent();

            _saveTimer = new System.Windows.Threading.DispatcherTimer {
                Interval = TimeSpan.FromSeconds(1)
            };
            _saveTimer.Tick += (s, e) => {
                if (_isTextDirty) {
                    SaveRichTextToModel();
                    _isTextDirty = false;
                }
                else
                    _saveTimer.Stop();
            };

            Loaded += (s, e) => {
                if (DataContext is ViewModels.MainViewModel vm && !string.IsNullOrEmpty(vm.StickyModel.Text)) {
                    _isLoading = true;

                    using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(vm.StickyModel.Text));

                    try {
                        var doc = (FlowDocument)System.Windows.Markup.XamlReader.Load(stream);
                        Editor.Document = doc;
                    }
                    catch {
                        stream.Position = 0;
                        try {
                            var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);
                            range.Load(stream, DataFormats.Rtf);
                        }
                        catch {
                            new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text = vm.StickyModel.Text;
                        }
                    }

                    _isLoading = false;
                    Editor.Document.PageWidth = Editor.ActualWidth;
                }
            };

            Unloaded += (s, e) => {
                if (_isTextDirty)
                    SaveRichTextToModel();
            };

            WeakReferenceMessenger.Default.Register<MainView, ImportExportMessage>(this, (r, m) => {
                if (r.DataContext != m.SourceViewModel)
                    return;

                switch (m.Action) {
                    case ImportExportMessage.Operation.Import: {
                        var dlg = new Microsoft.Win32.OpenFileDialog {
                            Filter = $"{Locales.Localizer.Instance["AllFormats"]}|*.txt;*.rtf;*.xaml|TXT (*.txt)|*.txt|RTF (*.rtf)|*.rtf|XAML (*.xaml)|*.xaml"
                        };

                        if (dlg.ShowDialog() is not true)
                            return;

                        string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();

                        try {
                            if (ext is ".xaml") {
                                using var stream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                                var doc = (FlowDocument)System.Windows.Markup.XamlReader.Load(stream);
                                r.Editor.Document = doc;
                            }
                            else if (ext is ".rtf") {
                                byte[] fileBytes = System.IO.File.ReadAllBytes(dlg.FileName);
                                string ascii = System.Text.Encoding.ASCII.GetString(fileBytes);

                                var match = System.Text.RegularExpressions.Regex.Match(ascii, @"\{\\\*\\stickynotexaml ([A-Za-z0-9+/=]+)\}");

                                bool loaded = false;
                                if (match.Success) {
                                    try {
                                        byte[] xamlBytes = Convert.FromBase64String(match.Groups[1].Value);
                                        using var xamlStream = new System.IO.MemoryStream(xamlBytes);
                                        r.Editor.Document = (FlowDocument)System.Windows.Markup.XamlReader.Load(xamlStream);
                                        loaded = true;
                                    }
                                    catch { }
                                }

                                if (!loaded) {
                                    r.Editor.Document.Blocks.Clear();
                                    var range = new TextRange(r.Editor.Document.ContentStart, r.Editor.Document.ContentEnd);
                                    using var stream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                                    range.Load(stream, DataFormats.Rtf);
                                    StripRtfMarkers(r.Editor.Document.Blocks);
                                }
                            }
                            else {
                                string text = System.IO.File.ReadAllText(dlg.FileName);
                                new TextRange(r.Editor.Document.ContentStart, r.Editor.Document.ContentEnd).Text = text;
                            }
                        }
                        catch { }

                        break;
                    }

                    case ImportExportMessage.Operation.Export: {
                        var dlg = new Microsoft.Win32.SaveFileDialog {
                            Filter = "TXT (*.txt)|*.txt|RTF (*.rtf)|*.rtf|XAML (*.xaml)|*.xaml",
                            FileName = "note",
                            DefaultExt = ".txt"
                        };

                        if (dlg.ShowDialog() is not true)
                            return;

                        string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();

                        try {
                            if (ext is ".xaml") {
                                using var stream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                                System.Windows.Markup.XamlWriter.Save(r.Editor.Document, stream);
                            }
                            else if (ext is ".rtf") {
                                var exportDoc = CloneDocument(r.Editor.Document);
                                PadEmptyParagraphsForRtf(exportDoc.Blocks);

                                var range = new TextRange(exportDoc.ContentStart, exportDoc.ContentEnd);
                                using var rtfStream = new System.IO.MemoryStream();
                                range.Save(rtfStream, DataFormats.Rtf);

                                using var xamlStream = new System.IO.MemoryStream();
                                System.Windows.Markup.XamlWriter.Save(r.Editor.Document, xamlStream);
                                string xamlBase64 = Convert.ToBase64String(xamlStream.ToArray());

                                byte[] rtfBytes = rtfStream.ToArray();
                                byte[] marker = System.Text.Encoding.ASCII.GetBytes("{\\*\\stickynotexaml " + xamlBase64 + "}");

                                int end = rtfBytes.Length;
                                while (end > 0 && rtfBytes[end - 1] is (byte)'\r' or (byte)'\n' or (byte)' ' or 0)
                                    end--;

                                using var outStream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                                outStream.Write(rtfBytes, 0, end - 1);
                                outStream.Write(marker, 0, marker.Length);
                                outStream.WriteByte((byte)'}');
                                outStream.Write(rtfBytes, end, rtfBytes.Length - end);
                            }
                            else {
                                string text = new TextRange(r.Editor.Document.ContentStart, r.Editor.Document.ContentEnd).Text;
                                System.IO.File.WriteAllText(dlg.FileName, text);
                            }
                        }
                        catch (Exception ex) {
                            System.Windows.MessageBox.Show(ex.ToString());
                        }

                            break;
                    }
                }

                r.Editor.Focus();
            });

            WeakReferenceMessenger.Default.Register<MainView, FindRequestMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                r.FindAndSelectText(m.SearchText);
            });

            WeakReferenceMessenger.Default.Register<MainView, ChangeFontFamilyMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                r.Editor.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, m.FontFamily);
            });

            WeakReferenceMessenger.Default.Register<MainView, ChangeFontSizeMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                r.Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, m.FontSize);
            });

            WeakReferenceMessenger.Default.Register<MainView, HyperlinkMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                if (m.Action == HyperlinkMessage.HyperlinkAction.Apply) {
                    string safeDisplayText = m.DisplayText ?? "";
                    string safeUrl = m.Url ?? "";

                    if (!string.IsNullOrWhiteSpace(safeUrl) &&
                        !safeUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !safeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        safeUrl = "https://" + safeUrl;

                    string textToInsert = string.IsNullOrEmpty(safeDisplayText) ? safeUrl : safeDisplayText;

                    Span? existingLink = r.GetSmartHyperlink(r.Editor.Selection);

                    if (existingLink is not null) {
                        r.Editor.Selection.Select(existingLink.ElementStart, existingLink.ElementEnd);
                    }
                    else {
                        Span? cutLink1 = r.GetHyperlinkFromPointer(r.Editor.Selection.Start);
                        Span? cutLink2 = r.GetHyperlinkFromPointer(r.Editor.Selection.End);

                        if (cutLink1 is not null) {
                            cutLink1.Tag = null;
                            cutLink1.Style = null;
                        }
                        if (cutLink2 is not null) {
                            cutLink2.Tag = null;
                            cutLink2.Style = null;
                        }
                    }

                    r.Editor.Selection.Text = "";

                    try {
                        var run = new Run(textToInsert);
                        var link = new Span(run, r.Editor.Selection.Start) {
                            Tag = safeUrl,
                            Style = (Style)r.FindResource("CustomHyperlinkStyle")
                        };

                        var plainRun = new Run("", link.ElementEnd);

                        r.Editor.Selection.Select(plainRun.ContentEnd, plainRun.ContentEnd);

                        r.Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, DependencyProperty.UnsetValue);
                        r.Editor.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, DependencyProperty.UnsetValue);
                    }
                    catch { }
                }
                else if (m.Action == HyperlinkMessage.HyperlinkAction.Remove) {
                    Span? link = r.GetSmartHyperlink(r.Editor.Selection);
                    if (link is not null) {
                        string plainText = new TextRange(link.ContentStart, link.ContentEnd).Text;
                        r.Editor.Selection.Select(link.ElementStart, link.ElementEnd);
                        r.Editor.Selection.Text = plainText;

                        link.Tag = null;
                        link.Style = null;
                    }
                }
                r.Editor.Focus();
            });

            WeakReferenceMessenger.Default.Register<MainView, BackupSelectionMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;
                r._colorBackupStream?.Dispose();

                if (r.Editor.Selection.IsEmpty) {
                    r._colorBackupStream = null;
                    return;
                }

                r._colorBackupStream = new System.IO.MemoryStream();
                r.Editor.Selection.Save(r._colorBackupStream, DataFormats.Rtf);

                r._backupStart = r.Editor.Selection.Start.GetPositionAtOffset(0, LogicalDirection.Forward);
                r._backupEnd = r.Editor.Selection.End.GetPositionAtOffset(0, LogicalDirection.Backward);
            });

            WeakReferenceMessenger.Default.Register<MainView, RestoreSelectionMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel || r._colorBackupStream is null)
                    return;

                if (r._backupStart is not null && r._backupEnd is not null) {
                    try {
                        r.Editor.Selection.Select(r._backupStart, r._backupEnd);
                    }
                    catch { }
                }

                r._colorBackupStream.Position = 0;
                r.Editor.Selection.Load(r._colorBackupStream, DataFormats.Rtf);
                r._colorBackupStream.Dispose();
                r._colorBackupStream = null;
            });

            WeakReferenceMessenger.Default.Register<MainView, ClearSelectionBackupMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                r._colorBackupStream?.Dispose();
                r._colorBackupStream = null;
            });

            WeakReferenceMessenger.Default.Register<MainView, ChangeFontColorMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                var brush = new System.Windows.Media.SolidColorBrush(m.NewColor);
                r.Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            });

            WeakReferenceMessenger.Default.Register<MainView, ChangeHighlightColorMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                var brush = new System.Windows.Media.SolidColorBrush(m.NewColor);
                r.Editor.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, brush);
            });

            Editor.SelectionChanged += (s, e) => {
                if (DataContext is ViewModels.MainViewModel vm) {
                    vm.HasSelection = !Editor.Selection.IsEmpty;

                    string text = Editor.Selection.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                        vm.SelectedText = text.Split('\r', '\n')[0];
                    else
                        vm.SelectedText = "";

                    var fgProperty = Editor.Selection.GetPropertyValue(TextElement.ForegroundProperty);
                    if (fgProperty is System.Windows.Media.SolidColorBrush fgBrush)
                        vm.SelectedFontColor = fgBrush.Color.ToString();
                    else if (Editor.Selection.Start.Parent is TextElement fgParent) {
                        var firstCharProp = fgParent.GetValue(TextElement.ForegroundProperty);
                        if (firstCharProp is System.Windows.Media.SolidColorBrush firstBrush)
                            vm.SelectedFontColor = firstBrush.Color.ToString();
                    }

                    var bgProperty = Editor.Selection.GetPropertyValue(TextElement.BackgroundProperty);
                    if (bgProperty is System.Windows.Media.SolidColorBrush bgBrush)
                        vm.SelectedHighlightColor = bgBrush.Color.ToString();
                    else if (Editor.Selection.Start.Parent is TextElement bgParent) {
                        var firstCharProp = bgParent.GetValue(TextElement.BackgroundProperty);
                        if (firstCharProp is System.Windows.Media.SolidColorBrush firstBrush)
                            vm.SelectedHighlightColor = firstBrush.Color.ToString();
                        else
                            vm.SelectedHighlightColor = "#00000000";
                    }

                    Span? foundLink = GetSmartHyperlink(Editor.Selection);
                    if (foundLink is not null && foundLink.Tag is string url) {
                        vm.SelectedHyperlinkUrl = url;
                        vm.SelectedText = new TextRange(foundLink.ContentStart, foundLink.ContentEnd).Text;
                    }
                    else {
                        vm.SelectedHyperlinkUrl = "";
                        string span = Editor.Selection.Text;
                        if (!string.IsNullOrWhiteSpace(span))
                            vm.SelectedText = span.Split('\r', '\n')[0];
                        else
                            vm.SelectedText = "";
                    }
                }
            };

            Editor.SizeChanged += (s, e) => {
                Editor.Document.PageWidth = Editor.ActualWidth;
            };

            Editor.TextChanged += (s, e) => {
                if (_isLoading)
                    return;

                CleanGhostLinks();

                _isTextDirty = true;
                if (!_saveTimer.IsEnabled)
                    _saveTimer.Start();
            };

            Editor.PreviewMouseLeftButtonDown += (s, e) => {
                if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control) {
                    var position = Editor.GetPositionFromPoint(e.GetPosition(Editor), true);
                    if (position is not null) {
                        var link = GetHyperlinkFromPointer(position);
                        if (link is not null && link.Tag is string url) {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                                FileName = url,
                                UseShellExecute = true
                            });
                            e.Handled = true;
                        }
                    }
                }
            };

            Editor.MouseMove += (s, e) => UpdateCursor();
            Editor.PreviewKeyDown += (s, e) => UpdateCursor();
            Editor.PreviewKeyUp += (s, e) => UpdateCursor();
        }

        private FlowDocument CloneDocument(FlowDocument source) {
            using var stream = new System.IO.MemoryStream();
            System.Windows.Markup.XamlWriter.Save(source, stream);
            stream.Position = 0;
            return (FlowDocument)System.Windows.Markup.XamlReader.Load(stream);
        }

        private void PadEmptyParagraphsForRtf(BlockCollection blocks) {
            foreach (var block in blocks) {
                if (block is Paragraph para) {
                    string text = new TextRange(para.ContentStart, para.ContentEnd).Text;
                    if (string.IsNullOrWhiteSpace(text)) {
                        var bg = (para.Inlines.FirstInline?.GetValue(TextElement.BackgroundProperty) as System.Windows.Media.Brush) ?? para.Background;

                        para.Inlines.Clear();
                        para.Inlines.Add(new Run(RtfMarkerText) {
                            Foreground = bg ?? System.Windows.Media.Brushes.Transparent,
                            Background = bg
                        });
                    }
                }
                else if (block is Section section)
                    PadEmptyParagraphsForRtf(section.Blocks);
            }
        }

        private void StripRtfMarkers(BlockCollection blocks) {
            foreach (var block in blocks) {
                if (block is Paragraph para)
                    StripRtfMarkersFromInlines(para.Inlines);
                else if (block is Section section)
                    StripRtfMarkers(section.Blocks);
            }
        }

        private void StripRtfMarkersFromInlines(InlineCollection inlines) {
            foreach (var inline in inlines.ToList()) {
                if (inline is Run run && run.Text.Contains(RtfMarkerText)) {
                    string cleaned = run.Text.Replace(RtfMarkerText, "");
                    var bg = run.Background;

                    inlines.InsertBefore(inline, new Run(cleaned.Length == 0 ? " " : cleaned) { Background = bg });
                    inlines.Remove(inline);
                }
                else if (inline is Span span)
                    StripRtfMarkersFromInlines(span.Inlines);
            }
        }

        private void UpdateCursor() {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control) {
                var pos = Editor.GetPositionFromPoint(System.Windows.Input.Mouse.GetPosition(Editor), true);
                if (pos is not null && GetHyperlinkFromPointer(pos) is not null) {
                    Editor.Cursor = System.Windows.Input.Cursors.Hand;
                    return;
                }
            }
            Editor.Cursor = System.Windows.Input.Cursors.IBeam;
        }

        private void SaveRichTextToModel() {
            if (DataContext is ViewModels.MainViewModel vm) {
                using var stream = new System.IO.MemoryStream();
                System.Windows.Markup.XamlWriter.Save(Editor.Document, stream);
                vm.StickyModel.Text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private void FindAndSelectText(string searchText) {
            if (string.IsNullOrEmpty(searchText)) return;

            TextPointer start = Editor.Selection.End;
            var foundRange = FindTextInRange(start, Editor.Document.ContentEnd, searchText);

            if (foundRange is null)
                foundRange = FindTextInRange(Editor.Document.ContentStart, Editor.Document.ContentEnd, searchText);

            if (foundRange is not null) {
                Editor.Selection.Select(foundRange.Start, foundRange.End);
                Editor.Focus();
                
                var rect = foundRange.Start.GetCharacterRect(LogicalDirection.Forward);
                Editor.ScrollToVerticalOffset(rect.Top + Editor.VerticalOffset - (Editor.ViewportHeight / 2));
            }
        }

        private TextRange? FindTextInRange(TextPointer start, TextPointer end, string keyword) {
            TextPointer position = start;
            
            while (position is not null && position.CompareTo(end) < 0) {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text) {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);
                    
                    int index = textRun.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0) {
                        TextPointer startPos = position.GetPositionAtOffset(index);
                        TextPointer endPos = startPos.GetPositionAtOffset(keyword.Length);
                        
                        if (endPos is not null && endPos.CompareTo(end) <= 0) {
                            return new TextRange(startPos, endPos);
                        }
                    }
                }
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
            return null;
        }

        private Span? GetSmartHyperlink(TextSelection selection) {
            Span? foundLink = GetHyperlinkFromPointer(selection.Start) ?? GetHyperlinkFromPointer(selection.End);

            if (foundLink is null && !selection.IsEmpty) {
                var insidePos = selection.Start.GetNextInsertionPosition(LogicalDirection.Forward);
                if (insidePos is not null && selection.Contains(insidePos))
                    foundLink = GetHyperlinkFromPointer(insidePos);
            }

            if (foundLink is not null && !selection.IsEmpty) {
                if (selection.Start.CompareTo(foundLink.ElementStart) < 0 ||
                    selection.End.CompareTo(foundLink.ElementEnd) > 0) {
                    return null;
                }
            }

            return foundLink;
        }

        private Span? GetHyperlinkFromPointer(TextPointer pointer) {
            DependencyObject parent = pointer.Parent;
            
            while (parent is Inline inline) {
                if (inline is Span span && span.Tag is string)
                    return span;
                parent = inline.Parent;
            }

            return null;
        }

        private void CleanGhostLinks() {
            var pointer = Editor.CaretPosition;
            if (pointer is null || GetHyperlinkFromPointer(pointer) is not null)
                return;

            var runs = new[] {
                    pointer.Parent as Run,
                    pointer.GetAdjacentElement(LogicalDirection.Forward) as Run,
                    pointer.GetAdjacentElement(LogicalDirection.Backward) as Run
                };

            foreach (var run in runs) {
                if (run is not null) {
                    if (run.ReadLocalValue(Inline.TextDecorationsProperty) != DependencyProperty.UnsetValue)
                        run.ClearValue(Inline.TextDecorationsProperty);

                    if (run.ReadLocalValue(TextElement.ForegroundProperty) is System.Windows.Media.SolidColorBrush b &&
                        b.Color == System.Windows.Media.Color.FromRgb(0, 102, 204))
                        run.ClearValue(System.Windows.Documents.TextElement.ForegroundProperty);
                }
            }
        }
    }
}
