using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.Spi;
using Microsoft.IoT.Lightning.Providers;
using SunfounderSensorKit.Base;

namespace SunfounderSensorKit.ViewModels
{
    public class I2CLcd160230 : ViewModelBase
    {
        private const int ControllerAddress = 0x27;
        private readonly bool enableBacklight;
        private const int Delay = 2;


        private string displayText;
        private I2cDevice i2CDevice;
        private SpiDevice spiDevice;

        public I2CLcd160230(bool backlight = true)
        {
            enableBacklight = backlight;
            SetupAsync().ConfigureAwait(false);
        }

        protected SpiController SpiController { get; set; }
        protected GpioController GpioController { get; set; }
        protected I2cController I2CController { get; set; }

        public string DisplayText
        {
            get => displayText;
            set
            {
                if (displayText == value)
                {
                    return;
                }

                displayText = value;

                if (value.Length > 0)
                {
                    WriteAsync(0, 0, value).ConfigureAwait(false);
                }

                //RaisePropertyChanged(DisplayText);
            }
        }

        private void WriteWord(int command)
        {
            if (enableBacklight)
            {
                command |= 0x08;
            }
            else
            {
                command &= 0xF7;
            }

            Debug.WriteLine($"Sending {command}");

            // Write
            //i2CDevice.Write(new[] {(byte)command });
            spiDevice.Write(new[] { (byte)command });

            //i2CDevice.Write(Encoding.ASCII.GetBytes(test.ToString()));

            //i2CDevice.Write(BitConverter.GetBytes(test));
        }

        public async Task SendCommandAsync(int command, int buffer = 0x04)
        {
            int buf = command & 0xF0;
            buf |= buffer; // # RS = 0, RW = 0, EN = 1
            WriteWord(buf);

            await Task.Delay(Delay).ConfigureAwait(true);

            buf &= 0xFB; //            # Make EN = 0
            WriteWord(buf);

            // # Send bit3-0 secondly
            buf = (command & 0x0F) << 4;
            buf |= buffer; //               # RS = 0, RW = 0, EN = 1
            WriteWord(buf);

            await Task.Delay(Delay).ConfigureAwait(true);

            buf &= 0xFB; //               # Make EN = 0
            WriteWord(buf);

            await Task.Delay(Delay).ConfigureAwait(true);
        }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);
            await ConfigureLcdAsync().ConfigureAwait(true);

            //await WriteAsync(0, 0, "Greetings!!").ConfigureAwait(false);
            //await WriteAsync(0, 1, "from SunFounder").ConfigureAwait(false);
        }


        public async Task WriteAsync(int x = 0, int y = 0, string data = "")
        {
            if (x < 0)
            {
                x = 0;
            }

            if (x > 15)
            {
                x = 15;
            }

            if (y < 0)
            {
                y = 0;
            }

            if (y > 1)
            {
                y = 1;
            }

            await SendCommandAsync(0x80 + 0x40 * y + x).ConfigureAwait(false);
//            await SendCommandAsync(0x01).ConfigureAwait(false);  // Clear Screen

            if (data.Trim().Length == 0)
            {
                return;
            }

            foreach (char c in data)
            {
                await SendCommandAsync(c, 0x05).ConfigureAwait(false);
            }
        }

        private async Task ConfigureLcdAsync()
        {
            await SendCommandAsync(0x33).ConfigureAwait(false); // Must initialize to 8-line mode at first

            await Task.Delay(Delay).ConfigureAwait(true);

            await SendCommandAsync(0x32).ConfigureAwait(false); // Then initialize to 4-line mode

            await Task.Delay(Delay).ConfigureAwait(true);

            await SendCommandAsync(0x28).ConfigureAwait(false); // 2 Lines & 5*7 dots

            await Task.Delay(Delay).ConfigureAwait(true);

            await SendCommandAsync(0x0C).ConfigureAwait(false); // Enable display without cursor

            await Task.Delay(Delay).ConfigureAwait(true);

            await SendCommandAsync(0x01).ConfigureAwait(false); // Clear Screen

            await Task.Delay(Delay).ConfigureAwait(true);

            await SendCommandAsync(0x08).ConfigureAwait(false); // 2 Lines

            await Task.Delay(Delay).ConfigureAwait(true);
        }

        private async Task InitializeControllersAsync()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Set PWM controller
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                I2CController = (await I2cController.GetControllersAsync(LightningI2cProvider.GetI2cProvider()))[0];
                i2CDevice = I2CController.GetDevice(new I2cConnectionSettings(ControllerAddress));

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                SpiController = (await SpiController.GetControllersAsync(LightningSpiProvider.GetSpiProvider()))[0];
                spiDevice = SpiController.GetDevice(new SpiConnectionSettings(1));

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