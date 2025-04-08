using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Basler.Pylon;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaApplication1.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {          
        //ObservableObject: MVVM 패턴에서 바인딩 속성에 대해 자동으로 INotifyPropertyChanged 구현
        private Camera? _camera;
        private CancellationTokenSource? _cts; // Grab 루프를 중지시키기 위한 토큰을 제공.
        private double _gainValue;
       
        [ObservableProperty] //자동으로 CameraImage라는 public 프로퍼티와 PropertyChanged 알림을 만들어줌
        private Bitmap? cameraImage;

        [RelayCommand] //이 메서드를 자동으로 커맨드로 만들어줌 → XAML에서 바인딩(UI와 코드 자동연결)가능하게.
        public Task StartCameraAsync()
        {
            try
            {
                _camera = new Camera();
                _camera.Open();
               
                _camera.StreamGrabber.Start();
                //파라미터설정
                _camera.Parameters[PLCamera.TriggerMode].SetValue("Off");
                _camera.Parameters[PLCamera.AcquisitionMode].SetValue("Continuous");
                _camera.Parameters[PLCamera.GainAuto].SetValue("Off");
                _cts = new CancellationTokenSource();
                Task.Run(() => GrabLoop(_cts.Token));  //무한 루프 함수를 백그라운드 스레드에서 실행
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartCamera Error: " + ex.Message);
            }
             return Task.CompletedTask; // RelayCommand가 Task 반환형이므로 빈 Task 반환
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
        public void SaveImage()
        {
            if (CameraImage == null)
                return;

            try
            {

                var path = @"C:\Users\unieye\source\repos\AvaloniaApplication1\AvaloniaApplication1\Assets\Captured.png";
                using var fs = new FileStream(path, FileMode.Create);
                CameraImage.Save(fs);
                Console.WriteLine("이미지 저장 성공!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("이미지 저장 실패: " + ex.Message);
            }
        }


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
                    using var grabResult = _camera?.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);//0.5초 안에 못 받으면 에러
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
