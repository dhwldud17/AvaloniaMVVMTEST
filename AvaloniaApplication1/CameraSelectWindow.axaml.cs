using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaApplication1.ViewModels;
using Basler.Pylon;
using System.Linq;
using System.Threading.Tasks;
namespace AvaloniaApplication1;

public partial class CameraSelectWindow : Window
{
    public string? SelectedSerial { get; private set; }

    public CameraSelectWindow() //창 처음만들때 실행되는 코드
    {
        InitializeComponent();
        // 샘플 카메라 리스트, 실제론 SDK에서 받아올 수 있음
        // Basler SDK로 연결된 카메라 시리얼 리스트 직접 가져오기

        Task.Run(() =>
        {
            var cameraSerials = CameraFinder.Enumerate()
            .Select(info => info[CameraInfoKey.SerialNumber])
            .ToList();

            Dispatcher.UIThread.InvokeAsync(() => //백그라운드에서 가져온 결과 UI 스레드에서 화면에 반영
            {
                CameraComboBox.ItemsSource = cameraSerials;
                if (cameraSerials.Any())
                    CameraComboBox.SelectedIndex = 0;
            });
        });
    }

    private void OnSelectClicked(object? sender, RoutedEventArgs e)
    {
        SelectedSerial = CameraComboBox.SelectedItem?.ToString();
        Close(SelectedSerial); // 부모 창으로 선택된 시리얼 넘버 전달
    }
}