namespace WindowsStickies.Models;

public class OpenAboutMessage { }
public class NewStickyMessage {
    public double SourceX { get; set; }
    public double SourceY { get; set; }
}

public class OpenFindTextMessage {
    public ViewModels.MainViewModel? SourceViewModel { get; set; }
}

public class OpenHyperlinkMessage {
    public ViewModels.MainViewModel? SourceViewModel { get; set; }
}

public class OpenColorPickerMessage {
    public ViewModels.MainViewModel? SourceViewModel { get; set; }
}

public class OpenFontPickerMessage {
    public ViewModels.MainViewModel? SourceViewModel { get; set; }
}

public class FindRequestMessage {
    public ViewModels.MainViewModel TargetViewModel { get; }
    public string SearchText { get; }

    public FindRequestMessage(ViewModels.MainViewModel targetViewModel, string searchText) {
        TargetViewModel = targetViewModel;
        SearchText = searchText;
    }
}
