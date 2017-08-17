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
    public class Test : ViewModelBase
    {
        private string displayText;
        private SpiDevice spiDevice;

        public Test()
        {
            SetupAsync().ConfigureAwait(true);
        }

        protected SpiController SpiController { get; set; }
        protected GpioController GpioController { get; set; }
        protected I2cController I2CController { get; set; }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);
            var a = Diagnostics.DeviceHelper.FindDevicesAsync();

        }

        private async Task InitializeControllersAsync()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Set PWM controller
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                I2CController = (await I2cController.GetControllersAsync(LightningI2cProvider.GetI2cProvider()))[0];
                I2CController.GetDevice(new I2cConnectionSettings(0x27));

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                SpiController = (await SpiController.GetControllersAsync(LightningSpiProvider.GetSpiProvider()))[0];
                SpiConnectionSettings spiConnectionSettings = new SpiConnectionSettings(0)
                {
                    ChipSelectLine = 1
                };
                spiDevice = SpiController.GetDevice(spiConnectionSettings);

                // Set GPIO controller
                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                GpioController = (await GpioController.GetControllersAsync(LightningGpioProvider.GetGpioProvider()))[0];
            }
            else
            {
                // Set default GPIO controller
                GpioController = GpioController.GetDefault(); /* Get the default GPIO controller on the system */

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                I2CController = await I2cController.GetDefaultAsync();

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                SpiController = await SpiController.GetDefaultAsync();
            }
        }
    }
}