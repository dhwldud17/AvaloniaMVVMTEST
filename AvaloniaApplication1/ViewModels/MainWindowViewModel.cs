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
using AvaloniaApplication1.Services;
using Microsoft.VisualBasic;

namespace AvaloniaApplication1.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        //ObservableObject: MVVM 패턴에서 바인딩 속성에 대해 자동으로 INotifyPropertyChanged 구현

        private readonly CameraManager _cameraManager;
        private readonly ImageManager _imageManager;

        [ObservableProperty]
        private WriteableBitmap? cameraImage;

        [ObservableProperty]
        private ObservableCollection<Bitmap> savedImages = new ObservableCollection<Bitmap>();

        [ObservableProperty]
        private double zoomLevel = 1.0;

        public MainWindowViewModel(CameraManager cameraManager, ImageManager imageManager)
        {
            _cameraManager = cameraManager;
            _imageManager = imageManager;

            // 카메라 이벤트와 바인딩 설정
            _cameraManager.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(CameraManager.CameraImage))
                {
                    CameraImage = _cameraManager.CameraImage;
                }

                if (args.PropertyName == nameof(CameraManager.SavedImages))
                {
                    SavedImages = _cameraManager.SavedImages;
                }
            };

            // 이미지 이벤트와 바인딩 설정
            _imageManager.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ImageManager.SavedImages))
                {
                    SavedImages = _imageManager.SavedImages;
                }
                if (args.PropertyName == nameof(ImageManager.ZoomLevel))
                {
                    ZoomLevel = _imageManager.ZoomLevel;
                }
            };
        }

        [RelayCommand]
        public void OpenCamera()
        {
            _cameraManager.OpenCamera();
        }

        [RelayCommand]
        public async Task CaptureFrame()
        {
            await _cameraManager.CaptureFrame();
            SavedImages = new ObservableCollection<Bitmap>(_cameraManager.SavedImages);
        }

        [RelayCommand]
        public async Task SaveImage()
        {
            await _imageManager.SaveImage();
        }

        [RelayCommand]
        public async Task LoadImageAsync()
        {
            await _imageManager.LoadImageAsync();
        }

        [RelayCommand]
        public void onMouseWheelZoom(double delta)
        {
            _imageManager.OnMouseWheelZoom(delta);
            ZoomLevel = _imageManager.ZoomLevel;
        }


        //[RelayCommand]
        //public void StopCamera()
        //{
        //    try
        //    {
        //        _cts?.Cancel();
        //        _camera?.StreamGrabber.Stop();
        //        _camera?.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("StopCamera Error: " + ex.Message);
        //    }
        //}


        //캡쳐된 이미지 리스트들을 SaveImage()누르면 해당 리스트들이 폴더로 저장됨. 



        //public double GainValue
        //{
        //    get => _gainValue;
        //    set
        //    {
        //        if (_gainValue != value)
        //        {
        //            _gainValue = value;
        //            OnPropertyChanged(nameof(GainValue));
        //            SetCameraGain(_gainValue);
        //        }
        //    }
        //}


        //// 실제 카메라 Gain 설정 함수
        //private void SetCameraGain(double gain)
        //{
        //    try
        //    {
        //        // Basler 카메라에서 Gain 설정
        //        if (_camera != null && _camera.Parameters[PLCamera.Gain].IsWritable)//IsWritable이 false?
        //        {
        //            // Gain 범위를 안전하게 설정
        //            double minGain = _camera.Parameters[PLCamera.Gain].GetMinimum();
        //            double maxGain = _camera.Parameters[PLCamera.Gain].GetMaximum();

        //            double clampedGain = Math.Clamp(gain, minGain, maxGain);
        //            _camera.Parameters[PLCamera.Gain].SetValue(clampedGain);
        //            Console.WriteLine($"Gain 설정됨: {clampedGain}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Gain 설정 실패: " + ex.Message);
        //    }
        //}
    }

}
