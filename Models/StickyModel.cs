using System;
using System.Windows;

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
    private WindowState _windowState = WindowState.Normal;

    [ObservableProperty]
    private bool _isTopmost = false;

    [ObservableProperty]
    private string _backgroundColor = "NavajoWhite";

    [ObservableProperty]
    private double _zoom = 1.0;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isRuledLines = false;
}
