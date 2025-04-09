using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaApplication1.ViewModels;
using Basler.Pylon;
using System.Linq;
namespace AvaloniaApplication1;

public partial class CameraSelectWindow : Window
{
    public string? SelectedSerial { get; private set; }

    public CameraSelectWindow()
    {
        InitializeComponent();
        // 샘플 카메라 리스트, 실제론 SDK에서 받아올 수 있음
        // Basler SDK로 연결된 카메라 시리얼 리스트 직접 가져오기
        var cameraSerials = CameraFinder.Enumerate()
            .Select(info => info[CameraInfoKey.SerialNumber])
            .ToList();

        CameraComboBox.ItemsSource = cameraSerials;

        if (cameraSerials.Any())
            CameraComboBox.SelectedIndex = 0;
    }

    private void OnSelectClicked(object? sender, RoutedEventArgs e)
    {
        SelectedSerial = CameraComboBox.SelectedItem?.ToString();
        Close(SelectedSerial); // 부모 창으로 선택된 시리얼 넘버 전달
    }
}