/**
  * This library implements basic liquid crystal library
  * that communicates with the Raspberry Pi 2 or Minnowboard Max
  * through I2C
  *
  * The functionality provided by this class is similar to 
  * Arduino LiquidCrystal library.
  * 
  * This software is furnished "as is", without technical support,
  * and with no warranty, express or implied, as to its usefulness 
  * for any purpose.
  *
  * Author: Daniel Vong Wei Liang (dvwl@hotmail.com)
  * Last modified: 10 December 2015
  *
  * Credits: WIndows IoT for their I2C I/O Port Expander example 
  * (https://www.hackster.io/4803/i2c-port-expander-sample)
  */

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace SunfounderSensorKit.Library
{
    public class I2CLcd
    {
        private readonly string cleanline = "";
        // commands
        private const int LcdCleardisplay = 0x01;
        private const int LcdReturnhome = 0x02;
        private const int LcdEntrymodeset = 0x04;
        private const int LcdDisplayControl = 0x08;
        private const int LcdCursorshift = 0x10;
        private const int LcdFunctionset = 0x20;
        private const int LcdSetcgramaddr = 0x40;
        private const int LcdSetddramaddr = 0x80;

        // flags for display entry mode
        private const int LcdEntryright = 0x00;
        private const int LcdEntryleft = 0x02;
        private const int LcdEntryshiftincrement = 0x01;
        private const int LcdEntryshiftdecrement = 0x00;

        // flags for display on/off control
        private const int LcdDisplayon = 0x04;
        private const int LcdDisplayoff = 0x00;
        private const int LcdCursoron = 0x02;
        private const int LcdCursoroff = 0x00;
        private const int LcdBlinkon = 0x01;
        private const int LcdBlinkoff = 0x00;

        // flags for display/cursor shift
        private const int LcdDisplaymove = 0x08;
        private const int LcdCursormove = 0x00;
        private const int LcdMoveright = 0x04;
        private const int LcdMoveleft = 0x00;

        // flags for function set
        private const int Lcd8Bitmode = 0x10;
        private const int Lcd4Bitmode = 0x00;
        private const int Lcd2Line = 0x08;
        private const int Lcd1Line = 0x00;
        public const int Lcd_5X10Dots = 0x04;
        public const int Lcd_5X8Dots = 0x00;

        // backlight
        private int lcdBacklight;

        private const int LcdBacklightOn = 0x08;

        //private const string I2C_Controller_Name = "I2C5"; // For Minnowboard Max
        private const string I2CControllerName = "I2C1";  // For Raspberry Pi 2

        // I2C Device declaration
        private I2cDevice i2C;

        private readonly int displayFunction;
        private int displayControl;
        private int displayMode;

        private readonly int addr;
        private readonly int cols;
        private readonly int rows;
        private int currentrow;

        private readonly string[] buffer;

        public bool AutoScroll = false;

        private const bool IsCommand = false;
        private const bool IsData = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelayMicroseconds(int uS)
        {
            if (uS > 2000)
                throw new Exception("Invalid param, use Task.Delay for 2ms and more");

            if (uS < 100) //call takes more time than 100uS 
                return;

            long tickToReach = DateTime.UtcNow.Ticks + uS * 1000; //1GHz Raspi2 Clock
            while (DateTime.UtcNow.Ticks < tickToReach)
            {
            }
        }

        public I2CLcd(int addr, int cols, int rows)
        {
            this.addr = addr;
            this.cols = cols;
            this.rows = rows;

            buffer = new string[rows];

            for (int i = 0; i < cols; i++)
            {
                cleanline = cleanline + " ";
            }

            if (this.rows > 1)
                displayFunction = displayFunction | Lcd2Line;
            else
                displayFunction = displayFunction | Lcd1Line;
        }

        private async Task LCD_SetupAsync()
        {
            try
            {
                string aqs = I2cDevice.GetDeviceSelector();
                DeviceInformationCollection dis = await DeviceInformation.FindAllAsync(aqs);

                if (dis.Count == 0)
                {
                    return;
                }

                I2cConnectionSettings settings = new I2cConnectionSettings(addr) {BusSpeed = I2cBusSpeed.StandardMode};
                i2C = await I2cDevice.FromIdAsync(dis[0].Id, settings);

                if (i2C == null)
                {
                }
            }
            catch
            {
                // ignored
            }
        }

        public async Task Begin()
        {
            await LCD_SetupAsync();

            // Pull both RS and R/W low to begin commands
            i2C.Write(new byte[] { (byte)addr, 0x00 });

            // start in 8bit mode, try to set 4 bit mode, Figure 24
            // https://www.sparkfun.com/datasheets/LCD/HD44780.pdf
            Initial4Bits(0x03);
            await Task.Delay(5); // wait min 4.1ms

            // second try
            Initial4Bits(0x03);
            await Task.Delay(5); // wait min 4.1ms

            // third go!
            Initial4Bits(0x03);
            DelayMicroseconds(150);

            // function set (set to be 4bits long)
            Initial4Bits(0x02);
            DelayMicroseconds(150);

            // Display off
            DisplayOff();

            // Display clear
            await ClearAsync();
            DelayMicroseconds(150);

            // Entry mode set
            Command(LcdEntrymodeset | LcdEntryleft);
            DelayMicroseconds(150);

            // Display On - Ready to go!
            DisplayOn();

            // 4-bit mode, 2 lines
            Command(0x28);
            DelayMicroseconds(150);

            // Turn backlight ON
            BacklightOn();
        }

        // Facilitate the 4 bit nibble of Figure 24 of https://www.sparkfun.com/datasheets/LCD/HD44780.pdf
        private void Initial4Bits(byte message)
        {
            try
            {
                i2C.Write(new[] { (byte)(message << 4 | lcdBacklight) });               // valid data comes first
                DelayMicroseconds(300);
                i2C.Write(new[] { (byte)((message << 4 | lcdBacklight) | 0x04) });      // En Pin HIGH 
                DelayMicroseconds(300);
                i2C.Write(new[] { (byte)((message << 4 & ~(0x04) | lcdBacklight)) });   // En Pin LOW
            }
            catch
            {
                // ignored
            }
        }
        // Writing to the LCD 4 bits at a time (4 bit mode)
        private void Write4Bits(byte message, bool registerSelect)
        {
            // RS = TRUE write to LCD (display text on LCD) => isData
            // RS = FALSE command to LCD (instruction to LCD) => isCommand
            // P0 of the I2C expander (PCF8574T) is connected to RS
            // Datasheet: http://www.nxp.com/documents/data_sheet/PCF8574.pdf

            // Send to I2C while toggling the En Pin of the LCD or P2 of the I2C expander 
            if (registerSelect)
            {
                // Data on LCD
                try
                {
                    //need to get P0 of I2C Device high
                    i2C.Write(new[] { (byte)(message | 0x01 | lcdBacklight) });      // valid data comes first, RS is high
                    //delayMicroseconds(300);
                    i2C.Write(new[] { (byte)(message | 0x05 | lcdBacklight) });      // En Pin HIGH
                    //delayMicroseconds(300);
                    i2C.Write(new[] { (byte)(message & ~(0x04) | lcdBacklight) });   // En Pin LOW
                }
                catch (Exception e)
                {
                    e.ToString();
                }
            }
            else
            {
                // Command to LCD
                try
                {
                    //need to get P0 of I2C Device low 
                    i2C.Write(new[] { (byte)(message | lcdBacklight) });             // valid data comes first
                    //delayMicroseconds(300);
                    i2C.Write(new[] { (byte)(message | 0x04 | lcdBacklight) });      // En Pin HIGH 
                    //delayMicroseconds(300);
                    i2C.Write(new[] { (byte)(message & ~(0x04) | lcdBacklight) });   // En Pin LOW
                }
                catch (Exception e)
                {
                    e.ToString();
                }
            }
        }

        // send data or command to LCD
        private void Send(byte message, bool registerSelect)
        {
            // higher nibble
            byte temp = (byte)((message >> 4) & 0x0F);
            Write4Bits((byte)(temp << 4), registerSelect);
            // lower nibble
            temp = (byte)(message << 4);
            Write4Bits(temp, registerSelect);
        }

        // send data to LCD
        private void Write(byte message)
        {
            Send(message, IsData);
        }

        // send command to LCD
        private void Command(byte message)
        {
            Send(message, IsCommand);
        }

        // clear LCD
        public async Task ClearAsync()
        {
            Command(LcdCleardisplay);
            await Task.Delay(2);

            for (int i = 0; i < rows; i++)
            {
                buffer[i] = "";
            }

            currentrow = 0;

            await HomeAsync();
        }

        // home LCD
        public async Task HomeAsync()
        {
            Command(LcdReturnhome);
            await Task.Delay(2);
        }

        // writing strings to LCD
        public void Write(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);

            foreach (byte ch in data)
            {
                Write(ch);
            }
        }

        // set cursor position
        public void SetCursor(byte col, byte row)
        {
            int[] rowOffsets = { 0x00, 0x40, 0x14, 0x54 };

            /*if (row >= _numlines)
            {
                row = _numlines - 1;    // we count rows starting w/0
            }
            */

            Command((byte)(LcdSetddramaddr | (col + rowOffsets[row])));
        }

        // Turn the backlight on/off
        public void BacklightOn()
        {
            lcdBacklight |= LcdBacklightOn;
        }
        public void BacklightOff()
        {
            lcdBacklight &= ~(LcdBacklightOn);
        }

        // Turn the display on/off (quickly)
        public void DisplayOn()
        {
            displayControl |= LcdDisplayon;
            Command((byte)(LcdDisplayControl | displayControl));
        }
        public void DisplayOff()
        {
            displayControl &= ~LcdDisplayon;
            Command((byte)(LcdDisplayControl | displayControl));
        }

        // Turns the underline cursor on/off
        public void NoCursor()
        {
            displayControl &= ~LcdCursoron;
            Command((byte)(LcdDisplayControl | displayControl));
        }
        public void Cursor()
        {
            displayControl |= LcdCursoron;
            Command((byte)(LcdDisplayControl | displayControl));
        }

        // Turn on and off the blinking cursor
        public void NoBlink()
        {
            displayControl &= ~LcdBlinkon;
            Command((byte)(LcdDisplayControl | displayControl));
        }
        public void Blink()
        {
            displayControl |= LcdBlinkon;
            Command((byte)(LcdDisplayControl | displayControl));
        }

        // These commands scroll the display without changing the RAM
        public void ScrollDisplayLeft()
        {
            Command(LcdCursorshift | LcdDisplaymove | LcdMoveleft);
        }
        public void ScrollDisplayRight()
        {
            Command(LcdCursorshift | LcdDisplaymove | LcdMoveright);
        }

        // This is for text that flows Left to Right
        public void LeftToRight()
        {
            displayMode |= LcdEntryleft;
            Command((byte)(LcdEntrymodeset | displayMode));
        }

        // This is for text that flows Right to Left
        public void RightToLeft()
        {
            displayMode &= ~LcdEntryleft;
            Command((byte)(LcdEntrymodeset | displayMode));
        }

        // This will 'right justify' text from the cursor
        public void Autoscroll()
        {
            displayMode |= LcdEntryshiftincrement;
            Command((byte)(LcdEntrymodeset | displayMode));
        }

        // This will 'left justify' text from the cursor
        public void NoAutoscroll()
        {
            displayMode &= ~LcdEntryshiftincrement;
            Command((byte)(LcdEntrymodeset | displayMode));
        }

        // Allows us to fill the first 8 CGRAM locations
        // with custom characters
        public void CreateChar(byte location, byte[] charmap)
        {
            location &= 0x7; // we only have 8 locations 0-7
            Command((byte)(LcdSetcgramaddr | (location << 3)));
            for (int i = 0; i < 8; i++)
            {
                Write(charmap[i]);
            }
        }

        public void WriteLine(string text)
        {
            if (currentrow >= rows)
            {
                //let's do shift
                for (int i = 1; i < rows; i++)
                {
                    buffer[i - 1] = buffer[i];
                    SetCursor(0, (byte)(i - 1));
                    Write(buffer[i - 1].Substring(0, cols));
                }
                currentrow = rows - 1;
            }
            buffer[currentrow] = text + cleanline;
            SetCursor(0, (byte)currentrow);
            string cuts = buffer[currentrow].Substring(0, cols);
            Write(cuts);
            currentrow++;
        }
    }
}
