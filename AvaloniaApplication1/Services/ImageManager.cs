using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Basler.Pylon;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Autofac;
using Autofac.Core;

namespace AvaloniaApplication1.Services
{
    public partial class ImageManager : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Bitmap> savedImages = [];

        [ObservableProperty]
        private double zoomLevel = 1.0;

        [RelayCommand]
        public async Task SaveImage()
        {
            if (SavedImages == null || SavedImages.Count == 0)
            {
                Console.WriteLine("저장할 이미지가 없습니다.");
                return;
            }

            try
            {
                var baseFolder = Path.Combine(
                     Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                     "MyCapturedImages");

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var saveFolder = Path.Combine(baseFolder, timestamp);
                Directory.CreateDirectory(saveFolder);

                await Task.Run(() =>
                {
                    for (int i = 0; i < SavedImages.Count; i++)
                    {
                        var image = SavedImages[i];
                        var filePath = Path.Combine(saveFolder, $"Captured_{i + 1}.png");

                        using var fs = new FileStream(filePath, FileMode.Create);
                        image.Save(fs);
                    }
                });

                Console.WriteLine("이미지 저장 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"이미지 저장 실패: {ex.Message}");
                return;
            }
        }

        [RelayCommand]
        public async Task LoadImageAsync()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;

                if (mainWindow == null)
                    return;

                var dialog = new OpenFolderDialog { Title = "이미지 폴더 선택" };
                var folder = await dialog.ShowAsync(parent: mainWindow);

                if (string.IsNullOrEmpty(folder))
                    return;

                string[] extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
                var imageFiles = Directory
                    .EnumerateFiles(folder)
                    .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));

                SavedImages.Clear();

                foreach (var file in imageFiles)
                {
                    using var stream = File.OpenRead(file);
                    var bitmap = new Bitmap(stream);
                    SavedImages.Add(bitmap);
                }
            }
        }

        public async Task SaveCapturedImage(byte[] imageBuffer)
        {
            if (imageBuffer == null || imageBuffer.Length == 0) return;

            var bitmap = await Task.Run(() => new Bitmap(new MemoryStream(imageBuffer)));
            SavedImages.Add(bitmap);
        }

        [RelayCommand]
        public void OnMouseWheelZoom(double delta)
        {
            const double zoomFactor = 1.1;

            if (delta > 0)
            {
                ZoomLevel *= zoomFactor;  // 줌 인
            }
            else
                ZoomLevel /= zoomFactor;  // 줌 아웃

            ZoomLevel = Math.Clamp(ZoomLevel, 0.5, 3.0);
        }

    }
}
