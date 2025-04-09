using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AvaloniaApplication1.ViewModels
{
    public partial class CameraSelectViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> availableCameras = new();

        [ObservableProperty]
        private string? selectedSerialNumber;

        public CameraSelectViewModel()
        {
            LoadAvailableCameras();
        }

        private void LoadAvailableCameras()
        {
            var cameras = Basler.Pylon.CameraFinder.Enumerate();
            AvailableCameras.Clear();

            foreach (var info in cameras)
            {
                if (info.ContainsKey(Basler.Pylon.CameraInfoKey.SerialNumber))
                {
                    AvailableCameras.Add(info[Basler.Pylon.CameraInfoKey.SerialNumber]);
                }
            }
        }
    }
}
