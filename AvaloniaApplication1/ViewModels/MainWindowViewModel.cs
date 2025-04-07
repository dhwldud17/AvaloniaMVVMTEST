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
    {          //ObservableObject: MVVM 패턴에서 바인딩 속성에 대해 자동으로 INotifyPropertyChanged 구현
        private Camera? _camera;
        private CancellationTokenSource? _cts; // Grab 루프를 중지시키기 위한 토큰을 제공.

        [ObservableProperty] //자동으로 CameraImage라는 public 프로퍼티와 PropertyChanged 알림을 만들어줌
        private Bitmap? cameraImage;

        [RelayCommand] //이 메서드를 자동으로 커맨드로 만들어줌 → XAML에서 바인딩(UI와 코드 자동연결)가능하게.
        public async Task StartCameraAsync()
        {
            try
            {
                _camera = new Camera();
                _camera.Open();
               
                _camera.StreamGrabber.Start();
                //트리거모드 off
                _camera.Parameters[PLCamera.TriggerMode].SetValue("Off");
                _camera.Parameters[PLCamera.AcquisitionMode].SetValue("Continuous");

                _cts = new CancellationTokenSource();
                await Task.Run(() => GrabLoop(_cts.Token));  //무한 루프 함수를 백그라운드 스레드에서 실행
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartCamera Error: " + ex.Message);
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

        private void GrabLoop(CancellationToken token)
        {
            var converter = new PixelDataConverter { OutputPixelFormat = PixelType.BGRA8packed };
            //Basler 카메라에서 받은 원시 데이터를 BGRA 형식으로 변환.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var grabResult = _camera?.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                    if (grabResult?.GrabSucceeded == true)
                    {
                        int width = grabResult.Width;
                        int height = grabResult.Height;
                        int stride = width * 4;
                        byte[] buffer = new byte[height * stride];
                        converter.Convert(buffer, grabResult);

                        Dispatcher.UIThread.InvokeAsync(() =>
                        { //UI 쓰레드에서 새로운 WriteableBitmap을 만들어서 픽셀 데이터 복사.
                            var wb = new WriteableBitmap(
                                new PixelSize(width, height),
                                new Vector(96, 96),
                                Avalonia.Platform.PixelFormat.Bgra8888,
                                Avalonia.Platform.AlphaFormat.Premul);

                            using (var fb = wb.Lock())
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
