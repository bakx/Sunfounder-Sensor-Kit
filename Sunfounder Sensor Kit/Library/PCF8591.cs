using System;
using System.Diagnostics;
using Windows.Devices.I2c;

namespace SunfounderSensorKit.Library
{
    public class Pcf8591
    {
        private I2cDevice i2CDevice;
        public string DeviceName { get; set; }

        public void Setup(I2cDevice device, string name)
        {
            i2CDevice = device;
            DeviceName = name;
        }

        public byte[] Read(byte address, int channel)
        {
            byte[] writeByte = { };

            try
            {
                switch (channel)
                {
                    case 0:
                        writeByte = new byte[] {0x40};
                        break;
                    case 1:
                        writeByte = new byte[] {0x41};
                        break;
                    case 2:
                        writeByte = new byte[] {0x42};
                        break;
                    case 3:
                        writeByte = new byte[] {0x43};
                        break;
                }

                i2CDevice.Write(writeByte);

                byte[] buffer = new byte[1];
                i2CDevice.Read(buffer);
                i2CDevice.Read(buffer);
                return buffer;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return writeByte;
        }
    }
}