using Avalonia.Controls;
using AvaloniaApplication1.ViewModels;

namespace AvaloniaApplication1.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // ViewModel ¿¬°á
        DataContext = new MainWindowViewModel();
    }
}