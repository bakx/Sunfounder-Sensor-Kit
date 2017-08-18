using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.Spi;
using Microsoft.IoT.Lightning.Providers;
using SunfounderSensorKit.Base;
using SunfounderSensorKit.Library;

namespace SunfounderSensorKit.ViewModels
{
    public class Thermistor80 : ViewModelBase
    {
        private const byte Address = 0x48;
        private const byte Address2 = 0x45;

        private Pcf8591 pcf8591;
        protected I2cController I2CController { get; set; }
        private I2cDevice i2CDevice;

        public Thermistor80()
        {
            SetupAsync().ConfigureAwait(true);
        }

        protected SpiController SpiController { get; set; }
        protected GpioController GpioController { get; set; }

        public async Task RunAsync(Pcf8591 pcf)
        {
            while (true)
            {
                try
                {
                    Debug.WriteLine(GetTemp(pcf, Address));
                    Debug.WriteLine(GetTemp(pcf, Address2));
                    
                    await Task.Delay(500).ConfigureAwait(true);
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
            return temp;
        }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);

            ConfigureSensors();

            await RunAsync(pcf8591).ConfigureAwait(false);
        }

        private void ConfigureSensors()
        {
            pcf8591 = new Pcf8591();
            pcf8591.Setup(i2CDevice, "#1");
        }

        private async Task InitializeControllersAsync()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Set PWM controller
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                I2CController = (await I2cController.GetControllersAsync(LightningI2cProvider.GetI2cProvider()))[0];
                i2CDevice = I2CController.GetDevice(new I2cConnectionSettings(Address));
            }
            else
            {
                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                I2CController = await I2cController.GetDefaultAsync();
            }
        }
    }
}