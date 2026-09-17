using System.Windows;
using System.Windows.Media;

namespace WindowsStickies.Views {
    public sealed class RuledLinesLayer : FrameworkElement {
        private static readonly Pen LinePen;
        private readonly List<double> _lines = new();

        static RuledLinesLayer() {
            var brush = new SolidColorBrush(Color.FromArgb(120, 150, 150, 150));
            brush.Freeze();
            LinePen = new Pen(brush, 1);
            LinePen.Freeze();
        }

        public RuledLinesLayer() {
            IsHitTestVisible = false;
            ClipToBounds = true;
        }

        public void SetLines(List<double> ys) {
            if (SameAs(ys))
                return;

            _lines.Clear();
            _lines.AddRange(ys);
            InvalidateVisual();
        }

        public void ClearLines() {
            if (_lines.Count is 0)
                return;

            _lines.Clear();
            InvalidateVisual();
        }

        private bool SameAs(List<double> ys) {
            if (ys.Count != _lines.Count)
                return false;

            for (int i = 0; i < ys.Count; i++)
                if (Math.Abs(ys[i] - _lines[i]) > 0.05)
                    return false;

            return true;
        }

        protected override void OnRender(DrawingContext dc) {
            if (_lines.Count is 0)
                return;

            double width = ActualWidth;
            double dpi = VisualTreeHelper.GetDpi(this).DpiScaleY;
            double half = 0.5 / dpi;

            foreach (double y in _lines) {
                double snapped = Math.Round(y * dpi) / dpi + half;
                dc.DrawLine(LinePen, new Point(0, snapped), new Point(width, snapped));
            }
        }
    }
}