using Avalonia.Controls;
using AvaloniaApplication1.ViewModels;
using Avalonia.Input;
using Avalonia;
using Avalonia.Interactivity;
using Autofac;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.Views;

public partial class MainWindow : Window  
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        // ViewModel ����
        DataContext = viewModel; // ViewModel�� DataContext�� ����
    }

    private void OnPointerWheelChanged(object sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        var viewModel = DataContext as ImageManager;
        if (viewModel == null) return;

        // ���콺�� ��ġ�� ��� ��ǥ (ZoomTarget ����)
        var mousePos = e.GetPosition(CameraImage);
        double originX = mousePos.X / CameraImage.Bounds.Width;
        double originY = mousePos.Y / CameraImage.Bounds.Height;

        // RenderTransformOrigin ���콺 �������� ����
        CameraImage.RenderTransformOrigin = new RelativePoint(originX, originY, RelativeUnit.Relative);

        // ViewModel���� �� ���� ȣ��
        viewModel.OnMouseWheelZoom(e.Delta.Y);
    }

    
}