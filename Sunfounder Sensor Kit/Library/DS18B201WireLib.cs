using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace SunfounderSensorKit.Library
{
    class WireSearchResult
    {
        public byte[] Id = new byte[8];
        public int LastForkPoint = 0;
    }
    public class OneWire
    {
        private SerialDevice serialPort;
        DataWriter dataWriteObject;
        DataReader dataReaderObject;

        public void Shutdown()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
                serialPort = null;
            }
        }

        async Task<bool> OnewireReset(string deviceId)
        {
            try
            {
                serialPort?.Dispose();

                serialPort = await SerialDevice.FromIdAsync(deviceId);

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                dataWriteObject = new DataWriter(serialPort.OutputStream);
                dataWriteObject.WriteByte(0xF0);
                await dataWriteObject.StoreAsync();

                dataReaderObject = new DataReader(serialPort.InputStream);
                await dataReaderObject.LoadAsync(1);
                byte resp = dataReaderObject.ReadByte();
                switch (resp)
                {
                    case 0xFF:
                        Debug.WriteLine("Nothing connected to UART");
                        return false;
                    case 0xF0:
                        Debug.WriteLine("No 1-wire devices are present");
                        return false;
                    default:
                        Debug.WriteLine("Response: " + resp);
                        serialPort.Dispose();
                        serialPort = await SerialDevice.FromIdAsync(deviceId);

                        // Configure serial settings
                        serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                        serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                        serialPort.BaudRate = 115200;
                        serialPort.Parity = SerialParity.None;
                        serialPort.StopBits = SerialStopBitCount.One;
                        serialPort.DataBits = 8;
                        serialPort.Handshake = SerialHandshake.None;
                        dataWriteObject = new DataWriter(serialPort.OutputStream);
                        dataReaderObject = new DataReader(serialPort.InputStream);
                        return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                return false;
            }
        }

        public async Task OnewireWriteByte(byte b)
        {
            for (byte i = 0; i < 8; i++, b = (byte)(b >> 1))
            {
                // Run through the bits in the byte, extracting the
                // LSB (bit 0) and sending it to the bus
                await OnewireBit((byte)(b & 0x01));
            }
        }

        async Task<byte> OnewireBit(byte b)
        {
            int bit = b > 0 ? 0xFF : 0x00;
            dataWriteObject.WriteByte((byte)bit);
            await dataWriteObject.StoreAsync();
            await dataReaderObject.LoadAsync(1);
            byte data = dataReaderObject.ReadByte();
            return (byte)(data & 0xFF);
        }

        async Task<byte> OnewireReadByte()
        {
            byte b = 0;
            for (byte i = 0; i < 8; i++)
            {
                // Build up byte bit by bit, LSB first
                b = (byte)((b >> 1) + 0x80 * await OnewireBit(1));
            }
            Debug.WriteLine("onewireReadByte result: " + b);
            return b;
        }

        public async Task<double> GetTemperature(string deviceId)
        {
            double tempCelsius = -200;

            if (await OnewireReset(deviceId))
            {
                await OnewireWriteByte(0xCC); //1-Wire SKIP ROM command (ignore device id)
                await OnewireWriteByte(0x44); //DS18B20 convert T command 
                // (initiate single temperature conversion)
                // thermal data is stored in 2-byte temperature 
                // register in scratchpad memory

                // Wait for at least 750ms for data to be collated
                await Task.Delay(750);

                // Get the data
                await OnewireReset(deviceId);
                await OnewireWriteByte(0xCC); //1-Wire Skip ROM command (ignore device id)
                await OnewireWriteByte(0xBE); //DS18B20 read scratchpad command
                // DS18B20 will transmit 9 bytes to master (us)
                // starting with the LSB

                byte tempLsb = await OnewireReadByte(); //read lsb
                byte tempMsb = await OnewireReadByte(); //read msb

                // Reset bus to stop sensor sending unwanted data
                await OnewireReset(deviceId);

                // Log the Celsius temperature
                tempCelsius = ((tempMsb * 256) + tempLsb) / 16.0;
                double temp2 = ((tempMsb << 8) + tempLsb) * 0.0625; //just another way of calculating it

                Debug.WriteLine("Temperature: " + tempCelsius + " degrees C " + temp2);
            }
            return tempCelsius;
        }
    }
}
