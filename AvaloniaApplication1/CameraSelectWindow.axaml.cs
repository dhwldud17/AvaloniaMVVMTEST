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

    public CameraSelectWindow() //â ó�����鶧 ����Ǵ� �ڵ�
    {
        InitializeComponent();
        // ���� ī�޶� ����Ʈ, ������ SDK���� �޾ƿ� �� ����
        // Basler SDK�� ����� ī�޶� �ø��� ����Ʈ ���� ��������

        Task.Run(() =>
        {
            var cameraSerials = CameraFinder.Enumerate()
            .Select(info => info[CameraInfoKey.SerialNumber])
            .ToList();

            Dispatcher.UIThread.InvokeAsync(() => //��׶��忡�� ������ ��� UI �����忡�� ȭ�鿡 �ݿ�
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
        Close(SelectedSerial); // �θ� â���� ���õ� �ø��� �ѹ� ����
    }
}