using System.Windows;
using System.Windows.Input;
using BazaarOverlay.Application.ViewModels;

namespace BazaarOverlay.WPF.Views;

public partial class CardOverlayWindow : Window
{
    private readonly CardOverlayViewModel _viewModel;

    public CardOverlayWindow(CardOverlayViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CardOverlayViewModel.IsVisible):
                if (_viewModel.IsVisible)
                {
                    Left = _viewModel.Left;
                    Top = _viewModel.Top;
                    Show();
                }
                else
                {
                    Hide();
                }
                break;

            case nameof(CardOverlayViewModel.CardUrl):
                if (_viewModel.CardUrl is not null)
                {
                    await WebView.EnsureCoreWebView2Async();
                    WebView.CoreWebView2.Navigate(_viewModel.CardUrl);
                }
                break;
        }
    }

    private void DragBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Hide();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _viewModel.Hide();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        _viewModel.Hide();
    }
}
