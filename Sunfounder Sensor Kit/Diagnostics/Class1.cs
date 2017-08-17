using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.I2c;
using Microsoft.IoT.Lightning.Providers;

namespace SunfounderSensorKit.Diagnostics
{
    class DeviceHelper
    {

        public static async Task<List<byte>> FindDevicesAsync()
        {
            IList<byte> returnValue = new List<byte>();
            const int minimumAddress = 1;
            const int maximumAddress = 77;

            // Set PWM controller
            LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

            // ReSharper disable once AsyncConverter.ConfigureAwaitHighlighting
            I2cController i2CController = (await I2cController.GetControllersAsync(LightningI2cProvider.GetI2cProvider()))[0];

            for (byte address = minimumAddress; address <= maximumAddress; address++)
            {
                I2cConnectionSettings settings =
                    new I2cConnectionSettings(address)
                    {
                        BusSpeed = I2cBusSpeed.FastMode,
                        SharingMode = I2cSharingMode.Shared
                    };

                // *** 
                // *** Create an I2cDevice with our selected bus controller and I2C settings 
                // *** 

                try
                {

                using (I2cDevice device = i2CController.GetDevice(settings))
                {
                    if (device != null)
                    {
                        try
                        {
                            byte[] writeBuffer = new byte[1] {0};
                            device.Write(writeBuffer);
                            // *** 
                            // *** If no exception is thrown, there is 
                            // *** a device at this address. 
                            // *** 
                            returnValue.Add(address);
                        }
                        catch
                        {
                            // *** 
                            // *** If the address is invalid, an exception will be thrown. 
                            // *** 
                        }
                    }
                }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return (List<byte>) returnValue;
        }
    }
}
