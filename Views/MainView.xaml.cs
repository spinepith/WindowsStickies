using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

using CommunityToolkit.Mvvm.Messaging;

using WindowsStickies.Models;


namespace WindowsStickies.Views {
    public partial class MainView : UserControl {
        private const double ScrollBarScreenWidth = 12;
        private const string RtfMarkerText = "\u00B7";

        private ScrollViewer? _editorViewport;
        private ScrollBar? _editorVerticalScrollBar;


        private System.Windows.Threading.DispatcherTimer _saveTimer;
        private bool _isTextDirty = false;
        private bool _isLoading = false;

        private System.IO.MemoryStream? _colorBackupStream;
        private TextPointer? _backupStart;
        private TextPointer? _backupEnd;
        private (Span Span, string Url, FontFamily FontFamily, double FontSize, object LocalForeground, object LocalBackground)? _backupHyperlink;

        private bool _clearFormattingOnNextInput = false;

        private bool _insertingTrailingSpaceLineBreak = false;

        private readonly List<double> _ruledLineBuffer = new();
        private bool _ruledLinesQueued;

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
                Editor.ApplyTemplate();
                _editorViewport = Editor.Template.FindName("PART_ContentHost", Editor) as ScrollViewer;
                UpdateEditorScrollBarWidth();

                if (_editorViewport is not null) {
                    _editorViewport.ScrollChanged += (sv, se) => {
                        if (se.ViewportWidthChange is not 0)
                            UpdateEditorPageWidth();

                        if (se.VerticalChange is not 0 || se.ExtentHeightChange is not 0 ||
                            se.ViewportHeightChange is not 0 || se.ViewportWidthChange is not 0)
                            InvalidateRuledLines();
                    };
                }

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

                if (DataContext is ViewModels.MainViewModel viewModel) {
                    viewModel.StickyModel.PropertyChanged += (sender, args) => {
                        if (args.PropertyName is nameof(viewModel.StickyModel.Zoom))
                            UpdateEditorScrollBarWidth();

                        if (args.PropertyName is nameof(viewModel.StickyModel.IsRuledLines) or nameof(viewModel.StickyModel.Zoom))
                            InvalidateRuledLines();
                    };
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
                                var range = new TextRange(r.Editor.Document.ContentStart, r.Editor.Document.ContentEnd);

                                if (ext is ".xaml") {
                                    using var stream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                                    System.Windows.Markup.XamlWriter.Save(r.Editor.Document, stream);
                                }
                                else if (ext is ".rtf") {
                                    using var stream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                                    range.Save(stream, DataFormats.Rtf);
                                }
                                else {
                                    string text = range.Text;
                                    System.IO.File.WriteAllText(dlg.FileName, text);
                                }
                            }
                            catch { }

                            break;
                        }

                        /* OLD EXPORT CODE */
                        //case ImportExportMessage.Operation.Export: {
                        //    var dlg = new Microsoft.Win32.SaveFileDialog {
                        //        Filter = "TXT (*.txt)|*.txt|RTF (*.rtf)|*.rtf|XAML (*.xaml)|*.xaml",
                        //        FileName = "note",
                        //        DefaultExt = ".txt"
                        //    };

                        //    if (dlg.ShowDialog() is not true)
                        //        return;

                        //    string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();

                        //    try {
                        //        if (ext is ".xaml") {
                        //            using var stream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                        //            System.Windows.Markup.XamlWriter.Save(r.Editor.Document, stream);
                        //        }
                        //        else if (ext is ".rtf") {
                        //            var exportDoc = CloneDocument(r.Editor.Document);
                        //            PadEmptyParagraphsForRtf(exportDoc.Blocks);

                        //            var range = new TextRange(exportDoc.ContentStart, exportDoc.ContentEnd);
                        //            using var rtfStream = new System.IO.MemoryStream();
                        //            range.Save(rtfStream, DataFormats.Rtf);

                        //            using var xamlStream = new System.IO.MemoryStream();
                        //            System.Windows.Markup.XamlWriter.Save(r.Editor.Document, xamlStream);
                        //            string xamlBase64 = Convert.ToBase64String(xamlStream.ToArray());

                        //            byte[] rtfBytes = rtfStream.ToArray();
                        //            byte[] marker = System.Text.Encoding.ASCII.GetBytes("{\\*\\stickynotexaml " + xamlBase64 + "}");

                        //            int end = rtfBytes.Length;
                        //            while (end > 0 && rtfBytes[end - 1] is (byte)'\r' or (byte)'\n' or (byte)' ' or 0)
                        //                end--;

                        //            using var outStream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                        //            outStream.Write(rtfBytes, 0, end - 1);
                        //            outStream.Write(marker, 0, marker.Length);
                        //            outStream.WriteByte((byte)'}');
                        //            outStream.Write(rtfBytes, end, rtfBytes.Length - end);
                        //        }
                        //        else {
                        //            string text = new TextRange(r.Editor.Document.ContentStart, r.Editor.Document.ContentEnd).Text;
                        //            System.IO.File.WriteAllText(dlg.FileName, text);
                        //        }
                        //    }
                        //    catch (Exception ex) {
                        //        MessageBox.Show(ex.ToString());
                        //    }

                        //    break;
                        //}
                }

                r.Editor.Focus();
            });

            WeakReferenceMessenger.Default.Register<MainView, EditCommandMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                switch (m.Command) {
                    case EditCommandMessage.EditCommand.Cut:
                        System.Windows.Input.ApplicationCommands.Cut.Execute(null, r.Editor);
                        break;

                    case EditCommandMessage.EditCommand.Copy:
                        System.Windows.Input.ApplicationCommands.Copy.Execute(null, r.Editor);
                        break;

                    case EditCommandMessage.EditCommand.Paste:
                        try {
                            var selection = r.Editor.Selection;

                            if (Clipboard.ContainsData(DataFormats.XamlPackage)) {
                                var stream = Clipboard.GetData(DataFormats.XamlPackage) as System.IO.MemoryStream;
                                if (stream is not null) {
                                    selection.Load(stream, DataFormats.XamlPackage);
                                    break;
                                }
                            }

                            if (Clipboard.ContainsData(DataFormats.Rtf)) {
                                using var stream = new System.IO.MemoryStream();
                                var rtfData = Clipboard.GetData(DataFormats.Rtf) as string;
                                if (rtfData is not null) {
                                    var bytes = System.Text.Encoding.ASCII.GetBytes(rtfData);
                                    stream.Write(bytes, 0, bytes.Length);
                                    stream.Position = 0;
                                    selection.Load(stream, DataFormats.Rtf);
                                    break;
                                }
                            }

                            if (Clipboard.ContainsText()) {
                                selection.Text = Clipboard.GetText();
                            }
                        }
                        catch {
                            System.Windows.Input.ApplicationCommands.Paste.Execute(null, r.Editor);
                        }
                        break;

                    case EditCommandMessage.EditCommand.PasteWithoutFormatting:
                        if (Clipboard.ContainsText()) {
                            string plainText = Clipboard.GetText();

                            r.Editor.Selection.Text = plainText;

                            TextPointer insertEnd = r.Editor.Selection.End;
                            TextPointer? insertStart = insertEnd.GetPositionAtOffset(-plainText.Length, LogicalDirection.Backward);

                            if (insertStart is not null)
                                new TextRange(insertStart, insertEnd).ClearAllProperties();

                            r._clearFormattingOnNextInput = true;
                        }
                        break;

                    case EditCommandMessage.EditCommand.Delete:
                        if (!r.Editor.Selection.IsEmpty)
                            r.Editor.Selection.Text = "";
                        else
                            System.Windows.Input.ApplicationCommands.Delete.Execute(null, r.Editor);
                        break;

                    case EditCommandMessage.EditCommand.SelectAll:
                        System.Windows.Input.ApplicationCommands.SelectAll.Execute(null, r.Editor);
                        break;
                }
                r.Editor.Focus();
            });

            WeakReferenceMessenger.Default.Register<MainView, ClearFormattingMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                if (!r.Editor.Selection.IsEmpty) {
                    r.Editor.Selection.ClearAllProperties();
                    r._clearFormattingOnNextInput = true;
                }
                else {
                    r._clearFormattingOnNextInput = true;
                }
                r.Editor.Focus();
            });

            WeakReferenceMessenger.Default.Register<MainView, FindRequestMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                r.FindAndSelectText(m.SearchText);
            });

            WeakReferenceMessenger.Default.Register<MainView, FontStyleMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                switch (m.StyleName) {
                    case "Bold":
                        var boldValue = r.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty);
                        if (boldValue != DependencyProperty.UnsetValue && (FontWeight)boldValue == FontWeights.Bold)
                            r.Editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                        else
                            r.Editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                        break;

                    case "Italic":
                        var italicValue = r.Editor.Selection.GetPropertyValue(TextElement.FontStyleProperty);
                        if (italicValue != DependencyProperty.UnsetValue && (FontStyle)italicValue == FontStyles.Italic)
                            r.Editor.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
                        else
                            r.Editor.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
                        break;

                    case "Underline":
                        ToggleTextDecoration(TextDecorationLocation.Underline, TextDecorations.Underline);
                        break;

                    case "Strikethrough":
                        ToggleTextDecoration(TextDecorationLocation.Strikethrough, TextDecorations.Strikethrough);
                        break;
                }

                void ToggleTextDecoration(TextDecorationLocation location, TextDecorationCollection decoration) {
                    var value = r.Editor.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
                    var current = value is TextDecorationCollection col ? col : new TextDecorationCollection();

                    bool exists = current.Any(d => d.Location == location);
                    var result = new TextDecorationCollection();

                    foreach (var item in current)
                        if (item.Location != location)
                            result.Add(item);

                    if (!exists)
                        foreach (var item in decoration)
                            result.Add(item);

                    r.Editor.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, result);
                }

                r.Editor.Focus();
            });

            WeakReferenceMessenger.Default.Register<MainView, ChangeFontFamilyMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                Span? link = r.GetSmartHyperlink(r.Editor.Selection);
                if (link is not null) {
                    link.FontFamily = m.FontFamily;
                    return;
                }

                r.Editor.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, m.FontFamily);
            });

            WeakReferenceMessenger.Default.Register<MainView, ChangeFontSizeMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                if (r.TryApplyFontSizeToHyperlink(System.Convert.ToDouble(m.FontSize)))
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
                            ClearFormatting(new TextRange(cutLink1.ContentStart, cutLink1.ContentEnd), TextElement.ForegroundProperty, Inline.TextDecorationsProperty);
                            cutLink1.Tag = null;
                            cutLink1.Style = null;
                        }
                        if (cutLink2 is not null) {
                            ClearFormatting(new TextRange(cutLink2.ContentStart, cutLink2.ContentEnd), TextElement.ForegroundProperty, Inline.TextDecorationsProperty);
                            cutLink2.Tag = null;
                            cutLink2.Style = null;
                        }
                    }

                    bool inheritFromSelection = existingLink is not null || !r.Editor.Selection.IsEmpty;
                    InlineFormatting formatting = r.CaptureInsertFormatting(inheritFromSelection);

                    if (!r.Editor.Selection.IsEmpty)
                        r.Editor.Selection.Text = "";

                    try {
                        var run = new Run(textToInsert);
                        var insertPos = r.Editor.Selection.Start.GetInsertionPosition(LogicalDirection.Forward)
                                        ?? r.Editor.Selection.Start;

                        var link = new Span(run, insertPos) {
                            Tag = safeUrl,
                            Style = (Style)r.FindResource("CustomHyperlinkStyle")
                        };

                        link.FontFamily = formatting.FontFamily;
                        link.FontSize = formatting.FontSize;
                        link.FontStyle = formatting.FontStyle;
                        link.FontWeight = formatting.FontWeight;

                        link.ClearValue(TextElement.ForegroundProperty);
                        link.ClearValue(TextElement.BackgroundProperty);
                        link.ClearValue(Inline.TextDecorationsProperty);

                        UnnestHyperlink(link);

                        var plainRun = new Run(string.Empty);
                        if (link.Parent is Paragraph paragraph)
                            paragraph.Inlines.InsertAfter(link, plainRun);
                        else if (link.Parent is Span parentSpan)
                            parentSpan.Inlines.InsertAfter(link, plainRun);

                        plainRun.ClearValue(TextElement.ForegroundProperty);
                        plainRun.ClearValue(TextElement.BackgroundProperty);
                        plainRun.ClearValue(Inline.TextDecorationsProperty);

                        r.Editor.CaretPosition = plainRun.ContentEnd;
                    }
                    catch { }
                }
                else if (m.Action == HyperlinkMessage.HyperlinkAction.Remove) {
                    Span? link = r.GetSmartHyperlink(r.Editor.Selection);
                    if (link is not null) {
                        string plainText = new TextRange(link.ContentStart, link.ContentEnd).Text;
                        r.Editor.Selection.Select(link.ElementStart, link.ElementEnd);
                        r.Editor.Selection.Text = plainText;

                        var newEnd = r.Editor.CaretPosition;
                        var newStart = newEnd.GetPositionAtOffset(-plainText.Length, LogicalDirection.Backward);
                        if (newStart is not null) {
                            var newRange = new TextRange(newStart, newEnd);
                            ClearFormatting(newRange, TextElement.ForegroundProperty, Inline.TextDecorationsProperty);
                        }

                        link.Tag = null;
                        link.Style = null;
                    }
                }
                r.Editor.Focus();
            });

            WeakReferenceMessenger.Default.Register<MainView, IncreaseFontSizeMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                Span? sizedLink = r.GetSmartHyperlink(r.Editor.Selection);

                if (r.Editor.Selection.IsEmpty && sizedLink is null)
                    return;

                double fontSize = 12.0;

                if (sizedLink is not null && !double.IsNaN(sizedLink.FontSize))
                    fontSize = sizedLink.FontSize;
                else if (r.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty) is double size)
                    fontSize = size;

                fontSize = Math.Min(fontSize + 1, 100);

                if (!r.TryApplyFontSizeToHyperlink(fontSize))
                    r.Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
            });

            WeakReferenceMessenger.Default.Register<MainView, DecreaseFontSizeMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                Span? sizedLink = r.GetSmartHyperlink(r.Editor.Selection);

                if (r.Editor.Selection.IsEmpty && sizedLink is null)
                    return;

                double fontSize = 12.0;

                if (sizedLink is not null && !double.IsNaN(sizedLink.FontSize))
                    fontSize = sizedLink.FontSize;
                else if (r.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty) is double size)
                    fontSize = size;

                fontSize = Math.Max(fontSize - 1, 8);

                if (!r.TryApplyFontSizeToHyperlink(fontSize))
                    r.Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
            });

            WeakReferenceMessenger.Default.Register<MainView, BackupSelectionMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;
                r._colorBackupStream?.Dispose();
                r._colorBackupStream = null;
                r._backupHyperlink = null;

                Span? link = r.GetSmartHyperlink(r.Editor.Selection);
                if (link is not null && link.Tag is string url) {
                    r._backupHyperlink = (link, url, link.FontFamily, link.FontSize,
                        link.ReadLocalValue(TextElement.ForegroundProperty),
                        link.ReadLocalValue(TextElement.BackgroundProperty));
                    return;
                }

                if (r.Editor.Selection.IsEmpty)
                    return;

                r._colorBackupStream = new System.IO.MemoryStream();
                r.Editor.Selection.Save(r._colorBackupStream, DataFormats.Rtf);
                r._backupStart = r.Editor.Selection.Start.GetPositionAtOffset(0, LogicalDirection.Forward);
                r._backupEnd = r.Editor.Selection.End.GetPositionAtOffset(0, LogicalDirection.Backward);
            });

            WeakReferenceMessenger.Default.Register<MainView, RestoreSelectionMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;
                if (r._backupHyperlink is { } hlBackup) {
                    hlBackup.Span.FontFamily = hlBackup.FontFamily;
                    hlBackup.Span.FontSize = hlBackup.FontSize;

                    if (hlBackup.LocalForeground == DependencyProperty.UnsetValue)
                        hlBackup.Span.ClearValue(TextElement.ForegroundProperty);
                    else if (hlBackup.LocalForeground is Brush fg)
                        hlBackup.Span.Foreground = fg;

                    if (hlBackup.LocalBackground == DependencyProperty.UnsetValue)
                        hlBackup.Span.ClearValue(TextElement.BackgroundProperty);
                    else if (hlBackup.LocalBackground is Brush bg)
                        hlBackup.Span.Background = bg;

                    r._backupHyperlink = null;
                    return;
                }

                if (r._colorBackupStream is null)
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
                r._backupHyperlink = null;
            });

            WeakReferenceMessenger.Default.Register<MainView, ChangeFontColorMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                var brush = new SolidColorBrush(m.NewColor);

                Span? link = r.GetSmartHyperlink(r.Editor.Selection);
                if (link is not null) {
                    link.Foreground = brush;
                    return;
                }

                r.Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            });

            WeakReferenceMessenger.Default.Register<MainView, ChangeHighlightColorMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                var brush = new SolidColorBrush(m.NewColor);

                Span? link = r.GetSmartHyperlink(r.Editor.Selection);
                if (link is not null) {
                    link.Background = brush;
                    return;
                }

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
                    if (fgProperty is SolidColorBrush fgBrush)
                        vm.SelectedFontColor = fgBrush.Color.ToString();
                    else if (Editor.Selection.Start.Parent is TextElement fgParent) {
                        var firstCharProp = fgParent.GetValue(TextElement.ForegroundProperty);
                        if (firstCharProp is SolidColorBrush firstBrush)
                            vm.SelectedFontColor = firstBrush.Color.ToString();
                    }

                    var bgProperty = Editor.Selection.GetPropertyValue(TextElement.BackgroundProperty);
                    if (bgProperty is SolidColorBrush bgBrush)
                        vm.SelectedHighlightColor = bgBrush.Color.ToString();
                    else if (Editor.Selection.Start.Parent is TextElement bgParent) {
                        var firstCharProp = bgParent.GetValue(TextElement.BackgroundProperty);
                        if (firstCharProp is SolidColorBrush firstBrush)
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
                UpdateEditorPageWidth();
                InvalidateRuledLines();
            };

            Editor.TextChanged += (s, e) => {
                if (_isLoading)
                    return;

                if (_clearFormattingOnNextInput)
                    ClearFormattingOnInsertedText(e);

                CleanGhostLinks();

                bool addedTrailingSpace = false;

                foreach (var change in e.Changes) {
                    if (change.AddedLength <= 0)
                        continue;

                    TextPointer? start =
                        Editor.Document.ContentStart.GetPositionAtOffset(
                            change.Offset,
                            LogicalDirection.Forward);

                    TextPointer? end =
                        start?.GetPositionAtOffset(
                            change.AddedLength,
                            LogicalDirection.Forward);

                    if (start is not null && end is not null) {
                        string addedText = new TextRange(start, end).Text;
                        if (addedText.EndsWith(' '))
                            addedTrailingSpace = true;
                    }
                }

                _isTextDirty = true;
                if (!_saveTimer.IsEnabled)
                    _saveTimer.Start();

                InvalidateRuledLines();

                // LINE BREAK
                if (addedTrailingSpace)
                    ForceTrailingSpaceWrap();
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
            Editor.PreviewKeyUp += (s, e) => UpdateCursor();

            Editor.PreviewKeyDown += (s, e) => {
                UpdateCursor();

                if (_clearFormattingOnNextInput && IsCaretNavigationKey(e.Key))
                    _clearFormattingOnNextInput = false;

                if (e.Key == System.Windows.Input.Key.Down && System.Windows.Input.Keyboard.Modifiers is System.Windows.Input.ModifierKeys.None or System.Windows.Input.ModifierKeys.Shift) {

                    Editor.CaretPosition.GetLineStartPosition(1, out int linesMoved);
                    if (linesMoved is 0) {
                        bool extendSelection = System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift;
                        var end = Editor.Document.ContentEnd;

                        if (extendSelection)
                            Editor.Selection.Select(Editor.Selection.Start, end);
                        else
                            Editor.Selection.Select(end, end);

                        Editor.CaretPosition = end;
                        e.Handled = true;
                    }
                }
            };

            Editor.PreviewMouseLeftButtonDown += (s, e) => _clearFormattingOnNextInput = false;
            Editor.PreviewKeyDown += (s, e) => {
                if (_clearFormattingOnNextInput && IsCaretNavigationKey(e.Key))
                    _clearFormattingOnNextInput = false;
            };
        }

        private void ForceTrailingSpaceWrap() {
            if (_insertingTrailingSpaceLineBreak || _isLoading || !Editor.Selection.IsEmpty)
                return;

            TextPointer caret = Editor.CaretPosition;

            int trailingSpaces = 0;
            TextPointer firstSpace = caret;

            while (firstSpace is not null) {
                char? character = GetPreviousTextCharacter(firstSpace);
                if (character is not ' ')
                    break;

                trailingSpaces++;

                TextPointer? previous =
                    firstSpace.GetNextInsertionPosition(LogicalDirection.Backward);

                if (previous is null)
                    break;

                firstSpace = previous;
            }

            if (trailingSpaces is 0)
                return;

            TextPointer? lineStart = caret.GetLineStartPosition(0);
            if (lineStart is null)
                return;

            Rect lineStartRect = lineStart.GetCharacterRect(LogicalDirection.Forward);
            if (lineStartRect.IsEmpty)
                return;

            double pageWidth = Editor.Document.PageWidth;

            if (double.IsNaN(pageWidth) ||
                double.IsInfinity(pageWidth) ||
                pageWidth <= 0) {
                pageWidth = _editorViewport?.ViewportWidth ?? Editor.ActualWidth;
            }

            if (pageWidth <= 0)
                return;

            Thickness padding = Editor.Document.PagePadding;
            double contentWidth = pageWidth - padding.Left - padding.Right;

            if (contentWidth <= 0)
                return;

            double lineLeft = lineStartRect.Left;
            double rightLimit = lineLeft + contentWidth;

            var spaceStarts = new List<TextPointer>(trailingSpaces);
            TextPointer? cursor = firstSpace;

            for (int i = 0; i < trailingSpaces && cursor is not null; i++) {
                spaceStarts.Add(cursor);
                cursor = cursor.GetNextInsertionPosition(LogicalDirection.Forward);
            }

            if (spaceStarts.Count is 0)
                return;

            double currentRight = lineLeft;

            TextPointer? beforeSpaces = firstSpace?.GetNextInsertionPosition(LogicalDirection.Backward);

            if (beforeSpaces is not null &&
                IsOnSameVisualLine(beforeSpaces, caret)) {
                Rect previousRect =
                    beforeSpaces.GetCharacterRect(LogicalDirection.Backward);

                if (!previousRect.IsEmpty)
                    currentRight = previousRect.Right;
            }

            for (int i = 0; i < spaceStarts.Count; i++) {
                double spaceWidth = MeasureSpaceWidth(spaceStarts[i]);

                if (spaceWidth <= 0)
                    return;

                currentRight += spaceWidth;

                const double rightEdgeSafetyMargin = 21;

                if (currentRight <= rightLimit - rightEdgeSafetyMargin)
                    continue;

                TextPointer breakPosition = spaceStarts[i];

                _insertingTrailingSpaceLineBreak = true;

                try {
                    TextPointer afterBreak = breakPosition.InsertLineBreak();
                    TextPointer newCaret = afterBreak;

                    for (int j = i; j < spaceStarts.Count; j++) {
                        TextPointer? next =
                            newCaret.GetNextInsertionPosition(LogicalDirection.Forward);

                        if (next is null)
                            break;

                        newCaret = next;
                    }

                    Editor.CaretPosition = newCaret;
                }
                finally {
                    _insertingTrailingSpaceLineBreak = false;
                }

                return;
            }
        }

        private static char? GetPreviousTextCharacter(TextPointer position) {
            TextPointer? probe = position;

            while (probe is not null) {
                char[] buffer = new char[1];

                int copied = probe.GetTextInRun(
                    LogicalDirection.Backward,
                    buffer,
                    0,
                    1);

                if (copied is 1)
                    return buffer[0];

                TextPointer? previous =
                    probe.GetNextContextPosition(LogicalDirection.Backward);

                if (previous is null || previous.CompareTo(probe) >= 0)
                    break;

                probe = previous;
            }

            return null;
        }

        private static bool IsOnSameVisualLine(TextPointer first, TextPointer second) {
            TextPointer? firstLine = first.GetLineStartPosition(0);
            TextPointer? secondLine = second.GetLineStartPosition(0);

            return firstLine is not null && secondLine is not null && firstLine.CompareTo(secondLine) is 0;
        }

        private double MeasureSpaceWidth(TextPointer spaceStart) {
            TextPointer? spaceEnd =
                spaceStart.GetNextInsertionPosition(LogicalDirection.Forward);

            if (spaceEnd is null)
                return 0;

            var range = new TextRange(spaceStart, spaceEnd);

            double fontSize = range.GetPropertyValue(TextElement.FontSizeProperty) is double size && size > 0 ? size : Editor.FontSize;
            FontFamily fontFamily = range.GetPropertyValue(TextElement.FontFamilyProperty) is FontFamily family ? family : Editor.FontFamily;
            FontStyle fontStyle = range.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle style ? style : Editor.FontStyle;
            FontWeight fontWeight = range.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight weight ? weight : Editor.FontWeight;
            FontStretch fontStretch = range.GetPropertyValue(TextElement.FontStretchProperty) is FontStretch stretch ? stretch : Editor.FontStretch;

            var typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);

            double pixelsPerDip = VisualTreeHelper.GetDpi(Editor).PixelsPerDip;

            var formattedText = new FormattedText(" ", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Transparent, pixelsPerDip);
            double width = formattedText.WidthIncludingTrailingWhitespace;

            double zoom = (DataContext as ViewModels.MainViewModel)?.StickyModel.Zoom ?? 1;
            if (zoom <= 0)
                zoom = 1;

            return width * zoom;
        }

        private void UpdateEditorPageWidth() {
            double width = _editorViewport?.ViewportWidth ?? Editor.ActualWidth;
            if (width > 0)
                Editor.Document.PageWidth = width;
        }

        private void UpdateEditorScrollBarWidth() {
            if (_editorViewport is null)
                return;

            _editorViewport.ApplyTemplate();
            _editorVerticalScrollBar ??= _editorViewport.Template.FindName("PART_VerticalScrollBar", _editorViewport) as ScrollBar;

            if (_editorVerticalScrollBar is null)
                return;

            double zoom = (DataContext as ViewModels.MainViewModel)?.StickyModel.Zoom ?? 1;
            _editorVerticalScrollBar.Width = ScrollBarScreenWidth / Math.Max(zoom, 0.01);
        }

        private static bool IsCaretNavigationKey(System.Windows.Input.Key key) {
            switch (key) {
                case System.Windows.Input.Key.Left:
                case System.Windows.Input.Key.Right:
                case System.Windows.Input.Key.Up:
                case System.Windows.Input.Key.Down:
                case System.Windows.Input.Key.Home:
                case System.Windows.Input.Key.End:
                case System.Windows.Input.Key.PageUp:
                case System.Windows.Input.Key.PageDown:
                    return true;
                default:
                    return false;
            }
        }

        private void ClearFormattingOnInsertedText(TextChangedEventArgs e) {
            try {
                foreach (var change in e.Changes) {
                    if (change.AddedLength <= 0)
                        continue;

                    TextPointer? start = Editor.Document.ContentStart.GetPositionAtOffset(change.Offset);
                    TextPointer? end = start?.GetPositionAtOffset(change.AddedLength);

                    if (start is null || end is null)
                        continue;

                    //if (GetHyperlinkFromPointer(start) is not null || GetHyperlinkFromPointer(end) is not null)
                    //    continue;

                    new TextRange(start, end).ClearAllProperties();
                }
            }
            catch { }
        }

        private static void ClearFormatting(TextRange range, params DependencyProperty[] properties) {
            if (range.IsEmpty)
                return;

            foreach (var prop in properties) {
                object current = range.GetPropertyValue(prop);
                if (current is not null && current != DependencyProperty.UnsetValue)
                    range.ApplyPropertyValue(prop, current);
            }

            for (TextPointer? p = range.Start; p is not null && p.CompareTo(range.End) <= 0; p = p.GetNextContextPosition(LogicalDirection.Forward)) {
                if (p.Parent is Inline inline && inline.ContentStart.CompareTo(range.End) < 0) {
                    foreach (var prop in properties)
                        inline.ClearValue(prop);
                }
            }
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
                        var bg = (para.Inlines.FirstInline?.GetValue(TextElement.BackgroundProperty) as Brush) ?? para.Background;

                        para.Inlines.Clear();
                        para.Inlines.Add(new Run(RtfMarkerText) {
                            Foreground = bg ?? Brushes.Transparent,
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

                    inlines.InsertBefore(inline, new Run(cleaned.Length is 0 ? " " : cleaned) { Background = bg });
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
            if (string.IsNullOrEmpty(searchText))
                return;

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

        private readonly record struct InlineFormatting(
            FontFamily FontFamily,
            double FontSize,
            FontStyle FontStyle,
            FontWeight FontWeight);

        private InlineFormatting CaptureInsertFormatting(bool fromSelection) {
            var defaults = new InlineFormatting(Editor.FontFamily, Editor.FontSize, FontStyles.Normal, FontWeights.Normal);

            TextRange? source = null;

            if (fromSelection && !Editor.Selection.IsEmpty)
                source = new TextRange(Editor.Selection.Start, Editor.Selection.End);
            else {
                TextPointer caret = Editor.Selection.Start;
                TextPointer? prev = caret.GetNextInsertionPosition(LogicalDirection.Backward);

                if (prev is not null && prev.Paragraph == caret.Paragraph && GetHyperlinkFromPointer(prev) is null)
                    source = new TextRange(prev, caret);
            }

            if (source is null)
                return defaults;

            return defaults with {
                FontSize = source.GetPropertyValue(TextElement.FontSizeProperty) is double size ? size : defaults.FontSize
            };
        }

        private bool TryApplyFontSizeToHyperlink(double fontSize) {
            Span? link = GetSmartHyperlink(Editor.Selection);
            if (link is null)
                return false;

            link.FontSize = fontSize;
            new TextRange(link.ContentStart, link.ContentEnd).ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);

            return true;
        }

        private static void UnnestHyperlink(Span link) {
            while (link.Parent is Span parent) {
                InlineCollection? host = parent.Parent switch {
                    Paragraph p => p.Inlines,
                    Span s => s.Inlines,
                    _ => null
                };
                if (host is null)
                    return;

                var before = new List<Inline>();
                var after = new List<Inline>();
                bool passed = false;

                foreach (Inline inline in parent.Inlines.Cast<Inline>().ToList()) {
                    if (ReferenceEquals(inline, link)) {
                        passed = true;
                        continue;
                    }
                    (passed ? after : before).Add(inline);
                }
                if (!passed)
                    return;

                parent.Inlines.Remove(link);
                foreach (var inline in before)
                    parent.Inlines.Remove(inline);
                foreach (var inline in after)
                    parent.Inlines.Remove(inline);

                Span? beforeSpan = null;
                Span? afterSpan = null;

                if (before.Count > 0) {
                    beforeSpan = new Span();
                    CopyInlineFormatting(parent, beforeSpan);
                    foreach (var inline in before) beforeSpan.Inlines.Add(inline);
                }
                if (after.Count > 0) {
                    afterSpan = new Span();
                    CopyInlineFormatting(parent, afterSpan);
                    foreach (var inline in after) afterSpan.Inlines.Add(inline);
                }

                host.InsertBefore(parent, link);
                if (beforeSpan is not null)
                    host.InsertBefore(link, beforeSpan);
                if (afterSpan is not null)
                    host.InsertAfter(link, afterSpan);
                host.Remove(parent);
            }
        }

        private static void CopyInlineFormatting(Span from, Span to) {
            var values = from.GetLocalValueEnumerator();
            while (values.MoveNext()) {
                var entry = values.Current;
                if (entry.Property.ReadOnly || entry.Property == FrameworkContentElement.NameProperty)
                    continue;
                if (entry.Value is System.Windows.Expression or System.Windows.Data.BindingExpressionBase)
                    continue;
                to.SetValue(entry.Property, entry.Value);
            }
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
                if (run is null)
                    continue;

                bool isLinkBlue = run.ReadLocalValue(TextElement.ForegroundProperty) is SolidColorBrush b && b.Color == Color.FromRgb(0, 102, 204);
                bool isUnderlined = run.ReadLocalValue(Inline.TextDecorationsProperty) is TextDecorationCollection col && col.Any(d => d.Location == TextDecorationLocation.Underline);

                if (isLinkBlue && isUnderlined) {
                    run.ClearValue(Inline.TextDecorationsProperty);
                    run.ClearValue(TextElement.ForegroundProperty);
                }
            }
        }

        private void InvalidateRuledLines() {
            if (_ruledLinesQueued)
                return;

            _ruledLinesQueued = true;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => {
                _ruledLinesQueued = false;
                DrawRuledLines();
            }));
        }

        private void DrawRuledLines() {
            if (DataContext is not ViewModels.MainViewModel vm || !vm.StickyModel.IsRuledLines) {
                RuledLinesCanvas.ClearLines();
                return;
            }

            double zoom = vm.StickyModel.Zoom <= 0 ? 1 : vm.StickyModel.Zoom;
            double localHeight = Editor.ActualHeight;

            if (localHeight <= 0 || ActualWidth <= 0) {
                RuledLinesCanvas.ClearLines();
                return;
            }

            var ys = _ruledLineBuffer;
            ys.Clear();

            try {
                Editor.UpdateLayout();
                TextPointer? pos = Editor.GetPositionFromPoint(new Point(1, 1), true) ?? Editor.Document.ContentStart;
                pos = pos.GetLineStartPosition(0) ?? pos;

                double lastBottom = double.NaN;
                int guard = 0;

                while (pos is not null && guard++ < 2000) {
                    var startRect = pos.GetCharacterRect(LogicalDirection.Forward);
                    if (startRect.IsEmpty || startRect.Top > localHeight)
                        break;

                    TextPointer? nextLine = pos.GetLineStartPosition(1, out int moved);
                    double bottom = MeasureLineBottom(pos, moved > 0 ? nextLine : null);

                    if (!double.IsNaN(bottom)) {
                        if (bottom >= 0 && bottom <= localHeight)
                            ys.Add(bottom);

                        lastBottom = bottom;
                    }

                    if (moved is 0 || nextLine is null || nextLine.CompareTo(pos) <= 0)
                        break;

                    pos = nextLine;
                }

                double baseLineHeight = 12 * 1.33;
                double startY = double.IsNaN(lastBottom) ? 0 : lastBottom;

                for (double y = startY + baseLineHeight; y < localHeight; y += baseLineHeight)
                    ys.Add(y);

                if (zoom is not 1)
                    for (int i = 0; i < ys.Count; i++)
                        ys[i] *= zoom;

                RuledLinesCanvas.SetLines(ys);
            }
            catch {
                RuledLinesCanvas.ClearLines();
            }
        }

        private static double MeasureLineBottom(TextPointer lineStart, TextPointer? lineEnd) {
            double bottom = double.NaN;
            var p = lineStart;
            int guard = 0;

            while (p is not null && guard++ < 64) {
                if (lineEnd is not null && p.CompareTo(lineEnd) >= 0)
                    break;

                var r = p.GetCharacterRect(LogicalDirection.Forward);
                if (!r.IsEmpty && (double.IsNaN(bottom) || r.Bottom > bottom))
                    bottom = r.Bottom;

                var next = p.GetNextContextPosition(LogicalDirection.Forward);
                if (next is null || next.CompareTo(p) <= 0)
                    break;

                p = next;
            }

            if (lineEnd is not null) {
                var last = lineEnd.GetNextInsertionPosition(LogicalDirection.Backward);
                if (last is not null) {
                    var r = last.GetCharacterRect(LogicalDirection.Backward);
                    if (!r.IsEmpty && (double.IsNaN(bottom) || r.Bottom > bottom))
                        bottom = r.Bottom;
                }
            }

            return bottom;
        }
    }
}