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

    public CameraSelectWindow()
    {
        InitializeComponent();
        // ���� ī�޶� ����Ʈ, ������ SDK���� �޾ƿ� �� ����
        // Basler SDK�� ����� ī�޶� �ø��� ����Ʈ ���� ��������
        Task.Run(() =>
        {
            var cameraSerials = CameraFinder.Enumerate()
            .Select(info => info[CameraInfoKey.SerialNumber])
            .ToList();

            Dispatcher.UIThread.InvokeAsync(() => //ī�޶� ���� UI �ȸ��߰� �ø��� ��ȣ�����ö� ��� ����ϵ���
            {
                // �� ���� �ڵ�� UI �����忡�� �����
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