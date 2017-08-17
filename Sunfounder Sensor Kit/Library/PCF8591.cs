using System;
using System.Diagnostics;
using Windows.Devices.I2c;
using Windows.Devices.Spi;

namespace SunfounderSensorKit.Library
{
    public class Pcf8591
    {
        private I2cDevice i2cDevice;
        private SpiDevice spiDevice;
        public string DeviceName { get; set; }

        public void Setup(SpiDevice device, string name)
        {
            spiDevice = device;
            DeviceName = name;
        }

        public byte[] Read(byte address, int channel)
        {
            byte[] writeByte = {0x1};

            try
            {
                switch (channel)
                {
                    case 0:
                        writeByte = new byte[] { address , 0x40};
                        break;
                    case 1:
                        writeByte = new byte[] { address, 0x41 };
                        break;
                    case 2:
                        writeByte = new byte[] { address, 0x42 };
                        break;
                    case 3:
                        writeByte = new byte[] { address, 0x43 };
                        break;
                }

                spiDevice.Write(writeByte);

                byte[] buffer = new byte[1];
                buffer[0] = address;
                byte[] readBuffer = new byte[1];
                spiDevice.TransferFullDuplex(buffer, readBuffer);
                return readBuffer;

                //byte[] buffer = new byte[2];
                //spiDevice.Write(writeByte);
                //spiDevice.Read(buffer);
                //i2cDevice.WriteRead(writeByte, buffer);

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
