using System.Windows;
using BazaarOverlay.Application.Interfaces;

namespace BazaarOverlay.WPF.Views;

public partial class DebugRectWindow : Window, IDebugRectWindow
{
    public DebugRectWindow()
    {
        InitializeComponent();
    }

    public void ShowRectangle(double x, double y, double width, double height)
    {
        Left = x;
        Top = y;
        Width = width;
        Height = height;
        Show();
    }

    public new void Hide()
    {
        base.Hide();
    }
}
