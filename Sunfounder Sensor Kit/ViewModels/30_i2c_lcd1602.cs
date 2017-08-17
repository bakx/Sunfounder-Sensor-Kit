using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Microsoft.IoT.Lightning.Providers;
using SunfounderSensorKit.Base;

namespace SunfounderSensorKit.ViewModels
{
    public class I2CLcd160230 : ViewModelBase
    {
        private const int ControllerAddress = 0x27;
        private readonly bool enableBacklight;
        private const int Delay = 20;

        private string displayText;
        private I2cDevice i2CDevice;

        public I2CLcd160230(bool backlight = true)
        {
            enableBacklight = backlight;
            SetupAsync().ConfigureAwait(false);
        }

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
                    SendCommand(0x01);
                    Write(0, 0, value);
                }
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
            i2CDevice.Write(new[] { (byte) command});
        }

        public void SendCommand(int command, int buffer = 0x04)
        {
            int buf = command & 0xF0;
            buf |= buffer; // # RS = 0, RW = 0, EN = 1
            WriteWord(buf);

            buf &= 0xFB; //            # Make EN = 0
            WriteWord(buf);

            // # Send bit3-0 secondly
            buf = (command & 0x0F) << 4;
            buf |= buffer; //               # RS = 0, RW = 0, EN = 1
            WriteWord(buf);

            buf &= 0xFB; //               # Make EN = 0
            WriteWord(buf);
        }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);
            await ConfigureLcdAsync().ConfigureAwait(true);

            Write(0, 0, "Greetings!!");
            Write(0, 1, "from SunFounder");
        }

        public void Write(int x = 0, int y = 0, string data = "")
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

            SendCommand(0x80 + 0x40 * y + x);

            foreach (char c in data)
            {
                SendCommand(c, 0x05);
            }
        }

        private async Task ConfigureLcdAsync()
        {
            SendCommand(0x33); // Must initialize to 8-line mode at first

            await Task.Delay(Delay).ConfigureAwait(true);

            SendCommand(0x32); // Then initialize to 4-line mode

            await Task.Delay(Delay).ConfigureAwait(true);

            SendCommand(0x28); // 2 Lines & 5*7 dots

            await Task.Delay(Delay).ConfigureAwait(true);

            SendCommand(0x0C); // Enable display without cursor

            await Task.Delay(Delay).ConfigureAwait(true);

            SendCommand(0x01); // Clear Screen

            await Task.Delay(Delay).ConfigureAwait(true);

            WriteWord(0x08);
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
            }
        }
    }
}