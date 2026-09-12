using System;
using System.Windows.Controls;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.Messaging;
using WindowsStickies.Models;

namespace WindowsStickies.Views {
    public partial class MainView : UserControl {
        public MainView() {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<MainView, FindRequestMessage>(this, (r, m) => {
                if (r.DataContext != m.TargetViewModel)
                    return;

                r.FindAndSelectText(m.SearchText);
            });
        }

        private void FindAndSelectText(string searchText) {
            if (string.IsNullOrEmpty(searchText)) return;

            TextPointer start = Editor.Selection.End;
            var foundRange = FindTextInRange(start, Editor.Document.ContentEnd, searchText);

            if (foundRange is null) {
                foundRange = FindTextInRange(Editor.Document.ContentStart, Editor.Document.ContentEnd, searchText);
            }

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
    }
}
