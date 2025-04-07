using ReactiveUI;
using System;
using System.Windows.Input;
using Tmds.DBus.Protocol;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Avalonia.Media.Imaging;
namespace AvaloniaApplication1.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        // 바인딩될 이미지 속성
        [ObservableProperty]
        private Bitmap? cameraImage;

        // Grab 버튼 클릭 시 실행될 명령
        [RelayCommand]
        private void Grab()
        {
            // 실제로는 Basler 카메라에서 이미지 받아오지만, 지금은 샘플 이미지로 테스트
            string imagePath = @"C:\Users\unieye\source\repos\AvaloniaApplication1\AvaloniaApplication1\Assets\sample.jpg"; ; 
            if (File.Exists(imagePath))
            {
                using var stream = File.OpenRead(imagePath);
                CameraImage = new Bitmap(stream);
                Console.WriteLine("Image loaded");
            }
            else
            {
                Console.WriteLine("Image not found: " + Path.GetFullPath(imagePath));
            }
        }
    }
}