using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using Basler.Pylon;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

//https://learn.microsoft.com/ko-kr/dotnet/csharp/tour-of-csharp/strategy : C#설명서 참고하기 
namespace AvaloniaApplication1.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        //ObservableObject: MVVM 패턴에서 바인딩 속성에 대해 자동으로 INotifyPropertyChanged 구현
        private Camera? _camera;
        private CancellationTokenSource? _cts; // Grab 루프를 중지시키기 위한 토큰을 제공.

        private bool _isStreaming = true;

        private double _gainValue;


        [ObservableProperty] //자동으로 CameraImage라는 public 프로퍼티와 PropertyChanged 알림을 만들어줌
        private Bitmap? cameraImage;

        [ObservableProperty]
        private ObservableCollection<string> availableCameras = new();

        [ObservableProperty]
        private string? selectedSerialNumber;

        [ObservableProperty]
        private ObservableCollection<Bitmap> savedImages = new();

        [ObservableProperty]
        private string toggleButtonText = "Capture"; // 버튼 텍스트 초기값

        [ObservableProperty]
        private IRelayCommand toggleStreamingCommand; // 스트리밍 시작/중지 커맨드

        public MainWindowViewModel()
        {
            // 스트리밍 시작/중지 커맨드 초기화
            ToggleStreamingCommand = new RelayCommand(ToggleStream);
        }

        [RelayCommand]
        private void ToggleStream()
        {
            if (_isStreaming)
            {
                //현재는 스트리밍 중이므로 스트리밍을 중지 -> 캡쳐 실행
                CaputureFrame();
                ToggleButtonText = "Streaming"; // 버튼 텍스트 변경  
            }
            else
            {
                //정지 상태 -> 다시 스트리밍 시작
                ResumeStreaming();
                ToggleButtonText = "Capture"; // 버튼 텍스트 변경
            }

            _isStreaming = !_isStreaming; // 스트리밍 상태 토글
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
                    List<ICameraInfo> allcaeras = CameraFinder.Enumerate();
                    AvailableCameras.Clear();

                    foreach (var info in allcaeras)
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

                _camera = new Camera(SelectedSerialNumber);
                _camera.Open();

                //파라미터설정
                //_camera.Parameters[PLCamera.GainAuto].SetValue("Off");
                //_camera.Parameters[PLCamera.Gain].SetValue(10.0);

                _camera.Parameters[PLCamera.TriggerMode].SetValue("Off");
                _camera.Parameters[PLCamera.AcquisitionMode].SetValue("Continuous");

                _cts = new CancellationTokenSource();
                _camera.StreamGrabber.Start();
                Task.Run(() => GrabLoop(_cts.Token));  //무한 루프 함수를 백그라운드 스레드에서 실행
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartCamera Error: " + ex.Message);
            }

            return Task.CompletedTask; // RelayCommand가 Task 반환형이므로 빈 Task 반환
        }

        //동작이 조금이라도 시간이 걸리는 작업이면 UI가 멈추지 않게하기위해 비동기(Task)로 만들어야 한다.
        //그래서 “캡처”처럼 실제 하드웨어를 다루거나  I/O(카메라, 파일, 네트워크) 관련 작업은 항상 Task 비동기로 처리하는 게 좋다.
        [RelayCommand]
        public void CaputureFrame()
        {
            if (_isStreaming == false || CameraImage == null)
            {
                return;
            }

            try
            {
                //스트리밍 중지
                _cts?.Cancel();
                _isStreaming = false;

                //현재 프레임을 메모리에 복사해서 리스트에 추가
                using var ms = new MemoryStream();
                CameraImage.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                var captured = new Bitmap(ms);

                SavedImages.Add(captured); // 리스트에 추가
                CameraImage = captured; // 현재 이미지를 캡처된 이미지로 변경
            }
            catch (Exception ex)
            {
                Console.WriteLine("CaptureFrame Error: " + ex.Message);
            }
        }

        [RelayCommand]
        public void ResumeStreaming()
        {
            if (_isStreaming)
            {
                return; // 이미 스트리밍 중이면 아무것도 하지 않음
            }

            try
            {
                // 스트리밍 재개
                _cts = new CancellationTokenSource();
                Task.Run(() => GrabLoop(_cts.Token));
                _isStreaming = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ResumeStreaming Error: " + ex.Message);
            }
        }

        [RelayCommand]
        public void StopCamera()
        {
            try
            {
                _cts?.Cancel();
                _camera?.StreamGrabber.Stop();
                _camera?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("StopCamera Error: " + ex.Message);
            }
        }

        [RelayCommand]
        public void LoadImage()
        {
            try
            {
                // 이미지 파일을 열고 Bitmap으로 변환
                var path = @"C:\Users\unieye\source\repos\AvaloniaApplication1\AvaloniaApplication1\Assets\Captured.png";
                using var fs = new FileStream(path, FileMode.Open);
                var loadedImage = new Bitmap(fs);
                CameraImage?.Dispose();
                CameraImage = loadedImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine("이미지 로드 실패: " + ex.Message);
            }
        }

        [RelayCommand]
        public void SaveImage()
        {
            if (CameraImage == null)
            {
                return;
            }

            try
            {
                var path = @"C:\Users\unieye\source\repos\AvaloniaApplication1\AvaloniaApplication1\Assets\Captured.png";
                using var fs = new FileStream(path, FileMode.Create);
                CameraImage.Save(fs);
                Console.WriteLine("이미지 저장 성공!");

                // 저장된 이미지를 리스트에 추가
                // CameraImage를 복제해서 넣어야 한다. WriteableBitmap은 재사용되므로
                using var ms = new MemoryStream();
                CameraImage.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                var copiedImage = new Bitmap(ms);

                SavedImages.Add(copiedImage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("이미지 저장 실패: " + ex.Message);
            }
        }

        //캡쳐 버튼 따로 -> 누르면 현재 카메라 화면 멈추고 해당 사진 옆에 리스트로 뜨게. 경로에 저장은 x ,그러고 UI는 Stream으로 변경됨.
        //Stream으로 변경된 버튼 다시 선택하면 라이브로 카메라가 변경되고 다시 UI는 capture로 변경됨.
        //save Images, Load Images 버튼따로. 
        //카메라 화면 줌인 아웃 기능 추가


        //속성 ->값이 변함
        // XAML은 함수 호출은 못 하고 오직 속성(Property) 에만 바인딩할 수 있다.
        public double GainValue
        {
            get => _gainValue;
            set
            {
                if (_gainValue != value)
                {
                    _gainValue = value;
                    OnPropertyChanged(nameof(GainValue));
                    SetCameraGain(_gainValue);
                }
            }
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

        // 실제 카메라 Gain 설정 함수
        private void SetCameraGain(double gain)
        {
            try
            {
                // Basler 카메라에서 Gain 설정
                if (_camera != null && _camera.Parameters[PLCamera.Gain].IsWritable)//IsWritable이 false?
                {
                    // Gain 범위를 안전하게 설정
                    double minGain = _camera.Parameters[PLCamera.Gain].GetMinimum();
                    double maxGain = _camera.Parameters[PLCamera.Gain].GetMaximum();

                    double clampedGain = Math.Clamp(gain, minGain, maxGain);
                    _camera.Parameters[PLCamera.Gain].SetValue(clampedGain);
                    Console.WriteLine($"Gain 설정됨: {clampedGain}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Gain 설정 실패: " + ex.Message);
            }
        }
    }

}
