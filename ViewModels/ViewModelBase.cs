using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowsStickies.ViewModels;

public abstract class ViewModelBase : ObservableObject {
    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
}