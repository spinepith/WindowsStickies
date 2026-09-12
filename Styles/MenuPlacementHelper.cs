using System.Windows;
using System.Windows.Controls.Primitives;

namespace WindowsStickies.Styles {
    public static class MenuPlacementHelper {
        private const double Gap = 9;

        public static CustomPopupPlacementCallback SubmenuPlacement { get; } =
            (popupSize, targetSize, offset) => {
                return new[] {
                    new CustomPopupPlacement(new Point(targetSize.Width + Gap, 0), PopupPrimaryAxis.Horizontal),
                    new CustomPopupPlacement(new Point(-popupSize.Width - Gap, 0), PopupPrimaryAxis.Horizontal)
                };
            };

        public static CustomPopupPlacementCallback TopLevelPlacement { get; } =
            (popupSize, targetSize, offset) => {
                return new[] {
                    new CustomPopupPlacement(new Point(targetSize.Width, 0), PopupPrimaryAxis.Horizontal),
                    new CustomPopupPlacement(new Point(-popupSize.Width, 0), PopupPrimaryAxis.Horizontal)
                };
            };
    }
}