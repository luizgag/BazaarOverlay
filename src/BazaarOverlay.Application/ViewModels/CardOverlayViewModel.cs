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
}
