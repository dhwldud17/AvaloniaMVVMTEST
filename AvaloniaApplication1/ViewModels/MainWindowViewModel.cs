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

namespace AvaloniaApplication1.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        //ObservableObject: MVVM 패턴에서 바인딩 속성에 대해 자동으로 INotifyPropertyChanged 구현
        private Camera? _camera;
        private CancellationTokenSource? _cts; // Grab 루프를 중지시키기 위한 토큰을 제공.

        private readonly ILifetimeScope _scope; // Autofac의 스코프를 사용하여 DI를 관리하기 위한 필드

        private const string TriggerModeOff = "Off"; // TriggerMode Off
        private const string AcquisitionModeContinuous = "Continuous"; // 연속 촬영 모드

        private bool _isStreaming = true;

        private double _gainValue;

        [ObservableProperty]
        private double zoomLevel = 1.0;

        [ObservableProperty] //자동으로 CameraImage라는 public 프로퍼티와 PropertyChanged 알림을 만들어줌
        private Bitmap? cameraImage;

        [ObservableProperty]
        private ObservableCollection<string> availableCameras = [];

        [ObservableProperty]
        private string? selectedSerialNumber;

        [ObservableProperty]
        private ObservableCollection<Bitmap> savedImages = [];

        //생성자에서 의존성주입
        public MainWindowViewModel(ILifetimeScope scope)
        {
            _scope = scope;
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

            return;
        }

        [RelayCommand] //이 메서드를 자동으로 커맨드로 만들어줌 → XAML에서 바인딩(UI와 코드 자동연결)가능하게.
        public Task StartCameraAsync()
        {
            try
            {
                //시리얼넘버 목록 보여주기
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
                _cts = _scope.Resolve<CancellationTokenSource>();
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
        public async Task CaputureFrame()
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
