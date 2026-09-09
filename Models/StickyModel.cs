using System;

using CommunityToolkit.Mvvm.ComponentModel;


namespace WindowsStickies.Models;

public partial class StickyModel : ObservableObject {
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    [ObservableProperty]
    private double _x = 100;
    
    [ObservableProperty]
    private double _y = 100;
    
    [ObservableProperty]
    private double _width = 280;
    
    [ObservableProperty]
    private double _height = 240;
    

    [ObservableProperty]
    private bool _isTopmost = false;
    

    [ObservableProperty]
    private string _text = "";
}
