using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Microsoft.IoT.Lightning.Providers;
using SunfounderSensorKit.Base;

namespace SunfounderSensorKit.ViewModels
{
    public class PassiveBuzzer10 : ViewModelBase
    {
        private const int Buzzer = 26; // GPIO Pin
        private readonly bool play;

        private static readonly int[] Cl = {0, 131, 147, 165, 175, 196, 211, 248}; // Frequency of Low C notes

        private static readonly int[] Cm = {0, 262, 294, 330, 350, 393, 441, 495}; // Frequency of Middle C notes

        private static readonly int[] Ch = {0, 525, 589, 661, 700, 786, 882, 990}; // Frequency of High C notes

        /// <summary>
        ///     Beats of song 1, 1 means 1/8 beats
        /// </summary>
        private readonly int[] beat1 =
        {
            1, 1, 3, 1, 1, 3, 1, 1,
            1, 1, 1, 1, 1, 1, 3, 1,
            1, 3, 1, 1, 1, 1, 1, 1,
            1, 2, 1, 1, 1, 1, 1, 1,
            1, 1, 3
        };

        /// <summary>
        ///     Beats of song 2, 1 means 1/8 beats
        /// </summary>
        private readonly int[] beat2 =
        {
            1, 1, 2, 2, 1, 1, 2, 2,
            1, 1, 2, 2, 1, 1, 3, 1,
            1, 2, 2, 1, 1, 2, 2, 1,
            1, 2, 2, 1, 1, 3
        };

        /// <summary>
        ///     Notes of song1
        /// </summary>
        private readonly int[] song1 =
        {
            Cm[3], Cm[5], Cm[6], Cm[3], Cm[2], Cm[3], Cm[5], Cm[6],
            Ch[1], Cm[6], Cm[5], Cm[1], Cm[3], Cm[2], Cm[2], Cm[3],
            Cm[5], Cm[2], Cm[3], Cm[3], Cl[6], Cl[6], Cl[6], Cm[1],
            Cm[2], Cm[3], Cm[2], Cl[7], Cl[6], Cm[1], Cl[5]
        };

        /// <summary>
        ///     Notes of song2
        /// </summary>
        private readonly int[] song2 =
        {
            Cm[1], Cm[1], Cm[1], Cl[5], Cm[3], Cm[3], Cm[3], Cm[1],
            Cm[1], Cm[3], Cm[5], Cm[5], Cm[4], Cm[3], Cm[2], Cm[2],
            Cm[3], Cm[4], Cm[4], Cm[3], Cm[2], Cm[3], Cm[1], Cm[1],
            Cm[3], Cm[2], Cl[5], Cl[7], Cm[2], Cm[1]
        };

        /// <summary>
        /// </summary>
        private PwmPin pwmPin;

        public PassiveBuzzer10(bool run = true)
        {
            play = run;
            SetupAsync().ConfigureAwait(false);
        }

        protected GpioController GpioController { get; set; }
        protected PwmController PwmController { get; set; }

        public void SetupPins()
        {
            GpioPin gpioPin = GpioController.OpenPin(Buzzer, GpioSharingMode.Exclusive);
            gpioPin.SetDriveMode(GpioPinDriveMode.Output);
            gpioPin.Write(GpioPinValue.High);

            pwmPin = PwmController.OpenPin(Buzzer);
            pwmPin.SetActiveDutyCyclePercentage(0.5);
            pwmPin.Controller.SetDesiredFrequency(440);
            pwmPin.Start();
        }

        public async Task RunAsync()
        {
            while (true)
            {
                Debug.WriteLine("Playing song 1");
                await PlaySoundAsync(song1, beat1).ConfigureAwait(true);

                Debug.WriteLine("Playing song 2");
                await PlaySoundAsync(song2, beat2).ConfigureAwait(true);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public Task BeepAsync(int frequence = 500)
        {
            return PlaySoundAsync(new[] { frequence }, new[] { 1 });
        }

        public async Task SetupAsync()
        {
            await InitializeControllersAsync().ConfigureAwait(true);
            SetupPins();

            if (play)
            {
                await RunAsync().ConfigureAwait(false);
            }
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

        private async Task PlaySoundAsync(IReadOnlyList<int> song, IReadOnlyList<int> beat)
        {
            for (int i = 1; i < song.Count; i++)
            {
                pwmPin.Controller.SetDesiredFrequency(song[i]);
                await Task.Delay(beat[i] * 500).ConfigureAwait(true);
            }

            await Task.Delay(1000).ConfigureAwait(true);
        }
    }
}