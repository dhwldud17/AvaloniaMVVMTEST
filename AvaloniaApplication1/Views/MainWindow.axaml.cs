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

        // ViewModel 연결
        DataContext = viewModel; // ViewModel을 DataContext로 설정
    }

    private void OnPointerWheelChanged(object sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        var viewModel = DataContext as ImageManager;
        if (viewModel == null) return;

        // 마우스가 위치한 상대 좌표 (ZoomTarget 기준)
        var mousePos = e.GetPosition(CameraImage);
        double originX = mousePos.X / CameraImage.Bounds.Width;
        double originY = mousePos.Y / CameraImage.Bounds.Height;

        // RenderTransformOrigin 마우스 기준으로 설정
        CameraImage.RenderTransformOrigin = new RelativePoint(originX, originY, RelativeUnit.Relative);

        // ViewModel에서 줌 로직 호출
        viewModel.OnMouseWheelZoom(e.Delta.Y);
    }

    
}