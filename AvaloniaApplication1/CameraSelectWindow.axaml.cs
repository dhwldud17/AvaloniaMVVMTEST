using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaApplication1.ViewModels;

namespace AvaloniaApplication1;

public partial class CameraSelectWindow : Window
{
    public string? SelectedSerialNumber => (DataContext as CameraSelectViewModel)?.SelectedSerialNumber;

    public CameraSelectWindow()
    {
        InitializeComponent();
        DataContext = new CameraSelectViewModel();
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Close(SelectedSerialNumber);
    }
}