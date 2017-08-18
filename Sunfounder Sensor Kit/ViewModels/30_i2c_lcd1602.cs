using System;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Microsoft.IoT.Lightning.Providers;
using SunfounderSensorKit.Base;
using SunfounderSensorKit.Library;

namespace SunfounderSensorKit.ViewModels
{
    public class I2CLcd160230 : ViewModelBase
    {
        private const int ControllerAddress = 0x27;
        private const int Delay = 20;

        private string displayText;
        private I2cDevice i2CDevice;
        private Lcd lcd;

        public I2CLcd160230()
        {
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
                    lcd.Write(0, 0, value);
                }
            }
        }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);

            lcd = new Lcd(Lcd.LcdType.Lcd1604, Delay, i2CDevice);
            await lcd.ConfigureLcdAsync().ConfigureAwait(true);

            lcd.Write(0, 0, "Greetings!!");
            lcd.Write(0, 1, "from SunFounder");
            lcd.Write(0, 2, "=================");
            lcd.Write(0, 3, "^_^");
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