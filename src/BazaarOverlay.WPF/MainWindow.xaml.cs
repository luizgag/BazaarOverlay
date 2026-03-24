using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BazaarOverlay.WPF.ViewModels;

namespace BazaarOverlay.WPF;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void ItemSkillList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string selected && DataContext is MainViewModel vm)
        {
            vm.ItemSkillInfo.SelectResult(selected);
        }
    }
}
