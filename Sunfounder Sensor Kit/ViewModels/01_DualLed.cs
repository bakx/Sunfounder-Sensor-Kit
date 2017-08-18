using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Microsoft.IoT.Lightning.Providers;
using SunfounderSensorKit.Base;

namespace SunfounderSensorKit.ViewModels
{
    public class DualLed01 : ViewModelBase
    {
        private readonly Dictionary<GpioPin, PwmPin> boardPins = new Dictionary<GpioPin, PwmPin>();

        private readonly int[] colors = {0xFF00, 0x00FF, 0x0FF0, 0xF00F};

        private readonly Dictionary<string, int> pins = new Dictionary<string, int> {{"R", 20}, {"G", 21}};

        public DualLed01()
        {
            SetupAsync().ConfigureAwait(false);
        }

        protected GpioController GpioController { get; set; }
        protected PwmController PwmController { get; set; }

        public void SetupPins()
        {
            foreach (KeyValuePair<string, int> pin in pins)
            {
                GpioPin gpioPin = GpioController.OpenPin(pin.Value, GpioSharingMode.Exclusive);
                gpioPin.SetDriveMode(GpioPinDriveMode.Output);
                gpioPin.Write(GpioPinValue.High);

                PwmPin pmwPin = PwmController.OpenPin(pin.Value);
                pmwPin.SetActiveDutyCyclePercentage(1.0);
                pmwPin.Controller.SetDesiredFrequency(200);
                pmwPin.Start();

                boardPins.Add(gpioPin, pmwPin);
            }
        }

        public async Task RunAsync()
        {
            while (true)
            {
                foreach (int col in colors)
                {
                    SetColor(col);
                    await Task.Delay(1000).ConfigureAwait(true);
                }

                Task.WaitAll();
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);
            SetupPins();
            await RunAsync().ConfigureAwait(false);
        }

        private async Task InitializeControllersAsync()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Set PWM controller
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                IReadOnlyList<PwmController> pwmControllers =
                    await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
                PwmController = pwmControllers[1]; // use the on-device controller

                // Set GPIO controller
                // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
                GpioController = (await GpioController.GetControllersAsync(LightningGpioProvider.GetGpioProvider()))[0];
            }
            else
            {
                // Set default GPIO controller
                GpioController = GpioController.GetDefault(); /* Get the default GPIO controller on the system */
            }
        }

        private static int Map(int x, int inMin, int inMax, int outMin, int outMax)
        {
            return (x - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }

        private void SetColor(int color)
        {
            int r = Map(color >> 8, 0, 255, 0, 100);
            int g = Map(color & 0x00FF, 0, 255, 0, 100);

            foreach (KeyValuePair<string, int> pin in pins)
            {
                double activeDutyCycle = 0;

                switch (pin.Key.ToUpper())
                {
                    case "R":
                        activeDutyCycle = r;
                        break;
                    case "G":
                        activeDutyCycle = g;
                        break;
                }

                activeDutyCycle = activeDutyCycle / 100;

                Debug.WriteLine($"Setting Duty Percentage To {activeDutyCycle} for pin {pin} and color {color}");

                KeyValuePair<GpioPin, PwmPin> boardPin = boardPins.Single(p => p.Key.PinNumber == pin.Value);
                boardPin.Value.SetActiveDutyCyclePercentage(activeDutyCycle);
            }
        }
    }
}