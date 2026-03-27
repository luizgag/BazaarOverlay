using CommunityToolkit.Mvvm.ComponentModel;

namespace BazaarOverlay.Application.ViewModels;

public partial class CardOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _cardUrl;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private double _left;

    [ObservableProperty]
    private double _top;

    [ObservableProperty]
    private bool _debugRectVisible;

    [ObservableProperty]
    private double _debugRectX;

    [ObservableProperty]
    private double _debugRectY;

    [ObservableProperty]
    private double _debugRectWidth;

    [ObservableProperty]
    private double _debugRectHeight;

    public void ShowCard(string url, double left, double top)
    {
        CardUrl = url;
        Left = left;
        Top = top;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }

    public void ShowDebugRect(double x, double y, double width, double height)
    {
        DebugRectX = x;
        DebugRectY = y;
        DebugRectWidth = width;
        DebugRectHeight = height;
        DebugRectVisible = true;
    }

    public void HideDebugRect()
    {
        DebugRectVisible = false;
    }
}
