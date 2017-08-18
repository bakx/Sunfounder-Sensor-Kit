using System;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.I2c;
using Microsoft.IoT.Lightning.Providers;
using SunfounderSensorKit.Base;
using SunfounderSensorKit.Library;

namespace SunfounderSensorKit.ViewModels
{
    public class Test : ViewModelBase
    {
        private const int DisplayAddress = 0x27;

        private const byte ThermalAddress = 0x48;
        private const byte ThermalAddress2 = 0x45;
        private I2cDevice displayDevice;

        private Lcd lcd;

        private Pcf8591 pcf8591;
        private I2cDevice thermalDevice;

        public Test()
        {
            SetupAsync().ConfigureAwait(true);
        }

        protected I2cController I2CController { get; set; }

        public async Task RunAsync(Pcf8591 pcf)
        {
            while (true)
            {
                try
                {
                    // Clear Display
                    displayDevice.Write(new byte[] {0x01});

                    lcd.Write(0, 0, $"Temp #1: {GetTemp(pcf, ThermalAddress)} C");
                    lcd.Write(0, 1, $"Temp #2: {GetTemp(pcf, ThermalAddress2)} C");
                    lcd.Write(0, 3, $"Time: {DateTime.Now:HH:mm:ss}");

                    await Task.Delay(750).ConfigureAwait(true);
                }
                catch
                {
                    // ignored
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static double GetTemp(Pcf8591 pcf, byte address)
        {
            byte[] a = pcf.Read(address, 1);
            double vr = 5 * Convert.ToDouble(a[0]) / 255;
            double rt = 10000 * vr / (5 - vr);
            double temp = 1 / (Math.Log(rt / 10000) / 3950 + 1 / (273.15 + 25));

            temp = temp - 273.15;
            return Math.Round(temp, 4);
        }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);

            ConfigureSensors();

            lcd = new Lcd(Lcd.LcdType.Lcd1604, 5, displayDevice);
            await lcd.ConfigureLcdAsync().ConfigureAwait(true);

            await RunAsync(pcf8591).ConfigureAwait(false);
        }

        private void ConfigureSensors()
        {
            pcf8591 = new Pcf8591();
            pcf8591.Setup(thermalDevice, "#1");
        }

        private async Task InitializeControllersAsync()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Set PWM controller
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                I2CController = (await I2cController.GetControllersAsync(LightningI2cProvider.GetI2cProvider()))[0];
                thermalDevice = I2CController.GetDevice(new I2cConnectionSettings(ThermalAddress));
                displayDevice = I2CController.GetDevice(new I2cConnectionSettings(DisplayAddress));
            }
            else
            {
                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                I2CController = await I2cController.GetDefaultAsync();
                thermalDevice = I2CController.GetDevice(new I2cConnectionSettings(ThermalAddress));
                displayDevice = I2CController.GetDevice(new I2cConnectionSettings(DisplayAddress));
            }
        }
    }
}