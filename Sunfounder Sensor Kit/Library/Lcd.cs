using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace SunfounderSensorKit.Library
{
    public class Lcd
    {
        public enum LcdType
        {
            Lcd1602,
            Lcd1604
        }

        private readonly int commandDelay;
        private readonly bool enableBacklight;

        private readonly I2cDevice i2CDevice;
        private readonly LcdType lcdType;

        public Lcd(LcdType type, int delay, I2cDevice device, bool backlight = true)
        {
            lcdType = type;
            commandDelay = delay;
            i2CDevice = device;
            enableBacklight = backlight;
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

            i2CDevice.Write(new[] {(byte) command});
        }

        public async Task ConfigureLcdAsync()
        {
            SendCommand(0x33); // Must initialize to 8-line mode at first

            await Task.Delay(commandDelay).ConfigureAwait(true);

            SendCommand(0x32); // Then initialize to 4-line mode

            await Task.Delay(commandDelay).ConfigureAwait(true);

            SendCommand(0x28); // 2 Lines & 5*7 dots

            await Task.Delay(commandDelay).ConfigureAwait(true);

            SendCommand(0x0C); // Enable display without cursor

            await Task.Delay(commandDelay).ConfigureAwait(true);

            SendCommand(0x01); // Clear Screen

            await Task.Delay(commandDelay).ConfigureAwait(true);

            WriteWord(0x08);
        }

        public void Write(int x = 0, int y = 0, string data = "")
        {
            int maxX = lcdType == LcdType.Lcd1602 ? 15 : 20;
            int maxY = lcdType == LcdType.Lcd1602 ? 2 : 4;

            if (x < 0)
            {
                x = 0;
            }

            if (x > maxX)
            {
                x = maxX;
            }

            if (y < 0)
            {
                y = 0;
            }

            if (y > maxY)
            {
                y = maxY;
            }

            // Fill up unused data with spaces
            if (data.Length < maxX)
            {
                data = data.PadRight(maxX - data.Length, ' ');
            }

            // Debug
            Debug.WriteLine($"Sending: {data}");

            // Set cursor position
            if (lcdType == LcdType.Lcd1602)
            {
                SendCommand(0x80 + 0x40 * y + x);
            }
            else if (lcdType == LcdType.Lcd1604)
            {
                switch (y)
                {
                    case 0:
                        SendCommand(0x80 + x);
                        break;
                    case 1:
                        SendCommand(0xC0 + x);
                        break;
                    case 2:
                        SendCommand(0x94 + x);
                        break;
                    case 3:
                        SendCommand(0xD4 + x);
                        break;
                }
            }

            // Send Data
            foreach (char c in data)
            {
                SendCommand(c, 0x05);
            }
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
    }
}