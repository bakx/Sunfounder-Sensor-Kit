using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Microsoft.IoT.Lightning.Providers;
using SunfounderSensorKit.Base;
using SunfounderSensorKit.Library;

namespace SunfounderSensorKit.ViewModels
{
    public class Thermistor80 : ViewModelBase
    {
        private const byte Address = 0x48;
        private Pcf8591 pcf8591;
        private GpioPin pin;
        private SpiDevice spiDevice;

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
                    for (byte channel = 0; channel <= 4; channel++)
                    {
                        byte[] a = pcf.Read(Address, channel);
                        double vr = 5 * Convert.ToDouble(a[0]) / 255;
                        double rt = 10000 * vr / (5 - vr);
                        double temp = 1 / (Math.Log(rt / 10000) / 3950 + 1 / (273.15 + 25));

                        temp = temp - 273.15;
                        Debug.WriteLine($"Sending to address {Address} and channel {channel}");
                        Debug.WriteLine(temp);

                        await Task.Delay(20).ConfigureAwait(true);
                    }
                }
                catch (Exception)
                {
                    //Debug.WriteLine(e);
                }
            }
        }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);

            ConfigureSensors();

            pin = GpioController.OpenPin(3);
            pin.SetDriveMode(GpioPinDriveMode.Input);

            await RunAsync(pcf8591).ConfigureAwait(false);
        }

        private void ConfigureSensors()
        {
            pcf8591 = new Pcf8591();
            pcf8591.Setup(spiDevice, "#1");
        }

        private async Task InitializeControllersAsync()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Set PWM controller
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                SpiController = (await SpiController.GetControllersAsync(LightningSpiProvider.GetSpiProvider()))[0];
                SpiConnectionSettings spiConnectionSettings = new SpiConnectionSettings(1)
                {
                    SharingMode = SpiSharingMode.Shared
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
                SpiController = await SpiController.GetDefaultAsync();
            }
        }
    }
}