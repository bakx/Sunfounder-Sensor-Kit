using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI.Xaml;
using SunfounderSensorKit.Base;
using SunfounderSensorKit.Library;

namespace SunfounderSensorKit.ViewModels
{
    public class Ds18B2026 : ViewModelBase
    {
        private OneWire onewire =  new OneWire();
        private string deviceId = string.Empty;
        private readonly DispatcherTimer timer;
        private bool inprog;
        public TemperatureModel TempData;

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }


        public Ds18B2026()
        {
            timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(1200)};
            timer.Tick += Timer_Tick;

            StartCommand = new CommandBase(null, StartBtn);
            StopCommand = new CommandBase(null, StopBtn);

        }

        private async void StartBtn(object o)
        {
            await GetFirstSerialPort();
            if (deviceId != string.Empty)
            {
                TempData.StatusText = "Reading from device: " + deviceId;
                TempData.Started = true;
                timer.Start();
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            if (!inprog)
            {
                inprog = true;
                TempData.Temperature = await onewire.GetTemperature(deviceId);
                inprog = false;
            }
        }

        private void StopBtn(object o)
        {
            timer.Stop();
            TempData.Started = false;
            onewire.Shutdown();
        }

        private async Task GetFirstSerialPort()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);
                if (dis.Count > 0)
                {
                    var deviceInfo = dis.First();
                    deviceId = deviceInfo.Id;
                }
            }
            catch (Exception ex)
            {
                TempData.StatusText = "Unable to get serial device: " + ex.Message;
            }
        }
    }
}