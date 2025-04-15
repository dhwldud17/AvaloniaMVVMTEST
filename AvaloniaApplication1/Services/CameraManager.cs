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
    public partial class CameraManager : ObservableObject
    {
        private Camera _camera { get; }
        private CancellationTokenSource? _cts;
        private readonly ILifetimeScope _scope;

        private const string TriggerModeOff = "Off"; // TriggerMode Off
        private const string AcquisitionModeContinuous = "Continuous"; // 연속 촬영 모드

        private bool _isStreaming = true;

        public CameraManager(ILifetimeScope scope)
        {
            _scope = scope;
        }

        [ObservableProperty]
        private ObservableCollection<Bitmap> savedImages = [];

        [ObservableProperty]
        private WriteableBitmap? cameraImage;

        [ObservableProperty]
        private string? selectedSerialNumber;

        [ObservableProperty]
        private ObservableCollection<string> availableCameras = [];

        [RelayCommand]
        public void OpenCamera() //Task뺌 
        {
            var cameraSelectWindow = new CameraSelectWindow();

            if (App.Current == null || App.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
            {
                return;
            }
            // 팝업 닫힐 때 처리할 이벤트 연결
            cameraSelectWindow.Closed += (sender, args) =>
            {
                var selectedSerial = cameraSelectWindow.SelectedSerial;

                if (!string.IsNullOrEmpty(selectedSerial))
                {
                    SelectedSerialNumber = selectedSerial;
                    StartCameraAsync();
                }
            };

            cameraSelectWindow.Show(desktop.MainWindow);
        }

        [RelayCommand] //이 메서드를 자동으로 커맨드로 만들어줌 → XAML에서 바인딩(UI와 코드 자동연결)가능하게.
        public Task StartCameraAsync()
        {
            try
            {
                if (SelectedSerialNumber == null)
                {
                    //연결된 카메라 목록 검색
                    List<ICameraInfo> allCameras = CameraFinder.Enumerate();
                    AvailableCameras.Clear();

                    foreach (var info in allCameras)
                    {
                        if (info.ContainsKey(CameraInfoKey.SerialNumber))
                        {
                            //카메라 시리얼넘버를 리스트에 추가
                            AvailableCameras.Add(info[CameraInfoKey.SerialNumber]);
                        }
                    }

                    if (AvailableCameras.Count == 0)
                    {
                        Console.WriteLine("카메라가 연결되어 있지 않습니다.");
                    }
                    else
                    {
                        Console.WriteLine("카메라 목록을 선택하세요.");
                    }

                    return Task.CompletedTask; // RelayCommand가 Task 반환형이므로 빈 Task 반환
                }

                //_camera = new Camera(SelectedSerialNumber);
                _camera = _scope.Resolve<Camera>(new NamedParameter("serial", SelectedSerialNumber));
                _camera.Open();

                //파라미터설정
                //_camera.Parameters[PLCamera.GainAuto].SetValue("Off");
                //_camera.Parameters[PLCamera.Gain].SetValue(10.0);

                _camera.Parameters[PLCamera.TriggerMode].SetValue(TriggerModeOff);
                _camera.Parameters[PLCamera.AcquisitionMode].SetValue(AcquisitionModeContinuous);

                // _cts = new CancellationTokenSource();
                _cts = new CancellationTokenSource();
                _camera.StreamGrabber.Start();
                Task.Run(() => GrabLoop(_cts.Token));  //무한 루프 함수를 백그라운드 스레드에서 실행
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartCamera Error: " + ex.Message);
            }

            return Task.CompletedTask;
        }




       [RelayCommand]
        public async Task CaptureFrame()
        {
            if (!_isStreaming || CameraImage == null)
                return;

            try
            {
                // 그냥 task걸면 UI쓰레드 충돌(?) 오류나서 분리시킴 근데그래도 UI 멈춤
                var buffer = await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    using var ms = new MemoryStream();
                    CameraImage.Save(ms);
                    return ms.ToArray();
                });

                // 백그라운드에서 Bitmap 생성
                var safeCopy = await Task.Run(() =>
                {
                    return new Bitmap(new MemoryStream(buffer));
                });

                // UI 스레드에서 리스트에 추가
                SavedImages.Add(safeCopy);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CaptureFrame Error: " + ex);
            }
        }

        public byte[] GetCurrentFrame()
        {
            if (CameraImage == null) return null;
            using var ms = new MemoryStream();
            CameraImage.Save(ms);
            return ms.ToArray();
        }

        private void GrabLoop(CancellationToken token)
        {
            var converter = new PixelDataConverter { OutputPixelFormat = PixelType.BGRA8packed };
            //Basler 카메라에서 받은 원시 데이터를 BGRA 형식으로 변환.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var grabResult = _camera?.StreamGrabber
                        .RetrieveResult(500, TimeoutHandling.ThrowException);//0.5초 안에 못 받으면 에러
                    //영상프레임하나 = GrabResult
                    if (grabResult?.GrabSucceeded == true)
                    {
                        int width = grabResult.Width;
                        int height = grabResult.Height;
                        int stride = width * 4; //BGRA 포맷은 한 픽셀당 4바이트 (Blue, Green, Red, Alpha)
                        byte[] buffer = new byte[height * stride]; //전체 이미지를 담을 버퍼 공간
                        converter.Convert(buffer, grabResult);

                        Dispatcher.UIThread.InvokeAsync(() =>
                        { //UI 쓰레드에서 새로운 WriteableBitmap을 만들어서 픽셀 데이터 복사.
                            var wb = new WriteableBitmap( // UI에서 사용할 수 있는 이미지를 만드는 객체
                                new PixelSize(width, height),
                                new Vector(96, 96), //DPI 설정
                                Avalonia.Platform.PixelFormat.Bgra8888, //픽셀 포맷 설정
                                Avalonia.Platform.AlphaFormat.Premul);  //알파 채널을 포함한 이미지 만드는 설정

                            using (var fb = wb.Lock()) //wb.Lock()은 WriteableBitmap을 잠그고, 해당 비트맵의 픽셀 데이터를 변경할 수 있는 메모리 영역을 제공
                            {
                                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, fb.Address, buffer.Length);
                            }

                            CameraImage?.Dispose();
                            CameraImage = wb;
                        });
                    }
                    else
                    {
                        Console.WriteLine("Grab failed or timed out");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("GrabLoop Error: " + ex.Message);
                }

                Thread.Sleep(10);  // 프레임 조절
            }
        }
    }
}
