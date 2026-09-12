using System.Windows.Input;

namespace WindowsStickies.Commands;

public static class RtbCommands {
    public static readonly RoutedCommand PasteWithoutFormatting = new(
        nameof(PasteWithoutFormatting), typeof(RtbCommands));

    public static readonly RoutedCommand InsertHyperlink = new(
        nameof(InsertHyperlink), typeof(RtbCommands));
}
