namespace WindowsStickies.Models;

public class OpenAboutMessage { }
public class NewStickyMessage {
    public double SourceX { get; set; }
    public double SourceY { get; set; }
}

public class OpenFindTextMessage {
    public ViewModels.MainViewModel? SourceViewModel { get; set; }
}

public class OpenColorPickerMessage {
    public ViewModels.MainViewModel? SourceViewModel { get; set; }
    public PickerMode Mode { get; set; }

    public enum PickerMode {
        FontColor,
        BackgroundColor,
        HighlightColor
    }
}

public class OpenHyperlinkMessage {
    public ViewModels.MainViewModel? SourceViewModel { get; set; }
}

///////////////////////////////////////////////////////////////////
public class FindRequestMessage {
    public ViewModels.MainViewModel TargetViewModel { get; }
    public string SearchText { get; }

    public FindRequestMessage(ViewModels.MainViewModel targetViewModel, string searchText) {
        TargetViewModel = targetViewModel;
        SearchText = searchText;
    }
}

public class ChangeFontColorMessage {
    public ViewModels.MainViewModel TargetViewModel { get; }
    public System.Windows.Media.Color NewColor { get; }

    public ChangeFontColorMessage(ViewModels.MainViewModel targetViewModel, System.Windows.Media.Color newColor) {
        TargetViewModel = targetViewModel;
        NewColor = newColor;
    }
}

public class ChangeHighlightColorMessage {
    public ViewModels.MainViewModel TargetViewModel { get; }
    public System.Windows.Media.Color NewColor { get; }

    public ChangeHighlightColorMessage(ViewModels.MainViewModel targetViewModel, System.Windows.Media.Color newColor) {
        TargetViewModel = targetViewModel;
        NewColor = newColor;
    }
}