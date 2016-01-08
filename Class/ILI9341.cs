using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Devices.Gpio;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace HomePi
{
    public class ILI9341Display
    {
        public string currentImage;
        public UInt16 LCD_VERTICAL_MAX = 320; // Y
        public UInt16 LCD_HORIZONTAL_MAX = 240; // X
        public static UInt16 BLACK = 0x0000;
        public static UInt16 WHITE = 0xFFFF;
        public static UInt16 VERTICAL_MAX_DEFAULT = 320; // y
        public static UInt16 HORIZONTAL_MAX_DEFAULT = 240; // X
        public SpiDevice SpiDisplay;
        public GpioPin DCPin;
        public GpioPin ResetPin;
        public UInt16 cursorX;
        public UInt16 cursorY;
        public byte[] DisplayBuffer; // A working pixel buffer for your code

        public ILI9341Display(int ulValue, int dcpin, int resetpin)
        {
            LCD_VERTICAL_MAX = VERTICAL_MAX_DEFAULT; // Y
            LCD_HORIZONTAL_MAX = HORIZONTAL_MAX_DEFAULT; // X

            //InitSPI(SpiDevice SPI, int SPIpin, int speed, SpiMode mode, string SPI_CONTROLLER_NAME)
            DCPin = ILI9341.InitGPIO(dcpin, GpioPinDriveMode.Output, GpioPinValue.High);
            ResetPin = ILI9341.InitGPIO(resetpin, GpioPinDriveMode.Output, GpioPinValue.High);
            cursorX = 0;
            cursorY = 0;
            DisplayBuffer = new byte[LCD_VERTICAL_MAX * LCD_HORIZONTAL_MAX * 2]; // A working pixel buffer for your code, RGB565
            currentImage = "";
        }

    }

    public static class ILI9341
    {
        private const string SPI_CONTROLLER_NAME = "SPI0";  /* For Raspberry Pi 2, use SPI0                             */
        private static readonly byte padding = 0x00;

        private static readonly byte[] CMD_SLEEP_OUT = { padding, 0x11 };
        private static readonly byte[] CMD_DISPLAY_ON = { padding, 0x29 };
        private static readonly byte[] CMD_MEMORY_WRITE_MODE = { padding, 0x2C };
        private static readonly byte[] CMD_DISPLAY_OFF = { padding, 0x28 };
        private static readonly byte[] CMD_ENTER_SLEEP = { padding, 0x10 };

        private static readonly byte[] CMD_COLUMN_ADDRESS_SET = { padding, 0x2a };
        private static readonly byte[] CMD_PAGE_ADDRESS_SET = { padding, 0x2b };
        private static readonly byte[] CMD_POWER_CONTROL_A = { padding, 0xcb };
        private static readonly byte[] CMD_POWER_CONTROL_B = { padding, 0xcf };
        private static readonly byte[] CMD_DRIVER_TIMING_CONTROL_A = { padding, 0xe8 };
        private static readonly byte[] CMD_DRIVER_TIMING_CONTROL_B = { padding, 0xea };
        private static readonly byte[] CMD_POWER_ON_SEQUENCE_CONTROL = { padding, 0xed };
        private static readonly byte[] CMD_PUMP_RATIO_CONTROL = { padding, 0xf7 };
        private static readonly byte[] CMD_POWER_CONTROL_1 = { padding, 0xc0 };
        private static readonly byte[] CMD_POWER_CONTROL_2 = { padding, 0xc1 };
        private static readonly byte[] CMD_VCOM_CONTROL_1 = { padding, 0xc5 };
        private static readonly byte[] CMD_VCOM_CONTROL_2 = { padding, 0xc7 };
        private static readonly byte[] CMD_MEMORY_ACCESS_CONTROL = { padding, 0x36 };
        private static readonly byte[] CMD_PIXEL_FORMAT = { padding, 0x3a };
        private static readonly byte[] CMD_FRAME_RATE_CONTROL = { padding, 0xb1 };
        private static readonly byte[] CMD_DISPLAY_FUNCTION_CONTROL = { padding, 0xb6 };
        private static readonly byte[] CMD_ENABLE_3G = { padding, 0xf2 };
        private static readonly byte[] CMD_GAMMA_SET = { padding, 0x26 };
        private static readonly byte[] CMD_POSITIVE_GAMMA_CORRECTION = { padding, 0xe0 };
        private static readonly byte[] CMD_NEGATIVE_GAMMA_CORRECTION = { padding, 0xe1 };
        private static readonly byte[] CMD_INTERFACE_MODE_CONTROL = { padding, 0xB0 };

        private static readonly byte[] CMD_DISPLAY_INVERSION_CONTROL = { padding, 0xB4 };
        private static readonly byte[] CMD_INTERFACE_PIXEL_FORMAT = { padding, 0x3a };
        private static readonly byte[] CMD_SET_IMAGE_FUNCTION = { padding, 0xE9 };
        private static readonly byte[] CMD_ENTRY_MODE_SET = { padding, 0xB7 };
        private static readonly byte[] CMD_ADJUST_CONTROL_3 = { padding, 0xf7 };
        private static readonly byte[] CMD_POSITIVE_GAMMA_CTRL = { padding, 0xe0 };
        private static readonly byte[] CMD_NEGATIVE_GAMMA_CTRL = { padding, 0xe1 };
        private static readonly byte[] CMD_FRAME_RATE_CONTROL_NORMAL = { padding, 0xb1 };
        private static bool AutoWrap = false;

        public static void write(ILI9341Display display, char[] c, byte textsize, int ulValue)
        {
            for (int len = 0; len < c.Length; len++)
            {
                write(display, c[len], textsize, ulValue);
            }
        }

        public static void Arc(ILI9341Display display, UInt16 lX, UInt16 lY, UInt16 Radius, UInt16 startAngle, UInt16 endAngle, int ulValue)
        {
            ArcEx(display, lX, lY, Radius, startAngle, endAngle, 0.1, ulValue);
        }
        // x, y are the center of the Arc
        public static void ArcEx(ILI9341Display display, UInt16 lX, UInt16 lY, UInt16 Radius, UInt16 startAngle, UInt16 endAngle, double increment, int ulValue)
        {
            double startA = startAngle;
            double endA = endAngle;
            if (startA > 360) startA = 360;
            if (endA > 360) endA = 360;
            if (increment > 10) increment = 10.0;
            if (increment < 0.1) increment = 0.1;

            for (double i = startA; i < endA; i += increment)
            {
                double angle = i * System.Math.PI / 180;
                UInt16 x = (UInt16)(lX + Radius * System.Math.Sin(angle));
                UInt16 y = (UInt16)(lY - Radius * System.Math.Cos(angle));
                PixelDraw(display, x, y, (UInt16)ulValue);
            }
        }

        public static void DrawCircle(ILI9341Display display, UInt16 x0, UInt16 y0, UInt16 radius, UInt16 ulValue)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius)
                        PixelDraw(display, (UInt16)(x + x0), (UInt16)(y + y0), ulValue);
            
        }
        public struct Graphics_Rectangle
        {
            public int XMin;
            public int XMax;
            public int YMin;
            public int YMax;
        }

        public static void write(ILI9341Display display, char c, byte textsize, int ulValue)
        {
            // bounds check
            if (display.cursorY >= display.LCD_VERTICAL_MAX) return; // were off the screen
            if (c == '\n') // do we have a new line, if so simply adjust cursor position
            {
                display.cursorY += (byte)(textsize * 8);   // next line based on font size
                display.cursorX = 0;               // back to  character 0
            }
            else if (c == '\r')
            {
                display.cursorX = 0;               // back to  character 0
            }
            else
            {
                drawChar(display, display.cursorX, display.cursorY, (byte)c, textsize, ulValue);
                display.cursorX += (UInt16)(textsize * 6);
                if (AutoWrap && (display.cursorX > (display.LCD_HORIZONTAL_MAX - textsize * 6)))
                {
                    display.cursorY += (byte)(textsize * 8);   // next line based on font size
                    display.cursorX = 0;               // back to  character 0
                }
            }
        }

        public static void PixelDraw(ILI9341Display display, UInt16 x, UInt16 y, int color)
        {
            if ( (y > 265) || (x > 265))
            {
                return;
            }
            


            if ((x < 0) || (x >= display.LCD_HORIZONTAL_MAX) || (y < 0) || (y >= display.LCD_VERTICAL_MAX)) return;
            byte hi = (byte)(color >> 8), low = (byte)(color & 0xff);
            SetAddress(display, x, y, (UInt16)(x + 1), (UInt16)(y + 1));
            SendCommand(display, CMD_MEMORY_WRITE_MODE);
            SendData(display, new byte[] { hi, low });
        }
        public static void drawChar(ILI9341Display display, UInt16 x, UInt16 y, byte c, UInt16 size, int ulValue)
        {
            if ((x >= display.LCD_HORIZONTAL_MAX) || (y >= display.LCD_VERTICAL_MAX) || ((x + 6 * size - 1) < 0) || ((y + 8 * size - 1) < 0))
                return;  // bounds checks

            for (byte i = 0; i < 6; i++) // 5*7 font + space
            {
                UInt16 line; // index into character font array
                if (i == 5) line = 0x0; // space between characters
                else line = DisplayFont.font[((c * 5) + i)];

                for (byte j = 0; j < 8; j++) // now process each bit of the character
                {// we have a bit and normal colour or no bit and inverted
                    if ((((line & 0x1) == 1) && (ulValue != 0)) || (((line & 0x1) == 0) && (ulValue == 0)))       // do we have a bit and black pixels (clear bits)         
                    {
                        if (size == 1) // default size
                            PixelDraw(display, (UInt16)(x + i), (UInt16)(y + j), ulValue);
                        else
                        {  // scaled up font
                            Graphics_Rectangle pRect;
                            pRect.XMin = x + (i * size);
                            pRect.YMin = y + (j * size);
                            pRect.XMax = pRect.XMin + size - 1;
                            pRect.YMax = pRect.YMin + size - 1;
                            fillRect(display, (UInt16)(x + (i * size)), (UInt16)(y + (j * size)), size, size, ulValue);
                        }
                    }
                    line >>= 1; // next bit in the line
                }
            }
        }


        public static void setCursor(ILI9341Display display, UInt16 x, UInt16 y)
        {
            // needs some bounds checking
            display.cursorX = (byte)x;
            display.cursorY = (byte)y;
        }

        public static void drawLine(ILI9341Display display, UInt16 x1, UInt16 y1, UInt16 x2, UInt16 y2, UInt16 ulValue)
        {
            UInt16 error, deltaX, deltaY;
            int yStep;
            bool SwapXY;

            // is this a vertical line as we can call an optimized routine for it
            if (x1 == x2)
            {
                // line drawV and H take a delta distance, not an absolute
                LineDrawV(display, x1, y1, (UInt16)(y2 - y1), ulValue);
                return;
            }
            // is this a horizontal line as we can call an optimized routine for it
            if (y1 == y2)
            {
                // line drawV and H take a delta distance, not an absolute
                LineDrawH(display, x1, y1, (UInt16)(x2 - x1), ulValue);
                return;
            }

            // Determine if the line is steep.  A steep line has more motion in the Y
            // direction than the X direction.
            if (((y2 > y1) ? (y2 - y1) : (y1 - y2)) > ((x2 > x1) ? (x2 - x1) : (x1 - x2)))
            {
                SwapXY = true;
            }
            else
            {
                SwapXY = false;
            }

            // If the line is steep, then swap the X and Y coordinates.
            if (SwapXY)
            {
                error = x1;
                x1 = y1;
                y1 = error;
                error = x2;
                x2 = y2;
                y2 = error;
            }

            //
            // If the starting X coordinate is larger than the ending X coordinate,
            // then swap the start and end coordinates.
            //
            if (x1 > x2)
            {
                error = x1;
                x1 = x2;
                x2 = error;
                error = y1;
                y1 = y2;
                y2 = error;
            }
            // Compute the difference between the start and end coordinates in each axis.
            deltaX = (UInt16)(x2 - x1);
            deltaY = (UInt16)((y2 > y1) ? (y2 - y1) : (y1 - y2));

            // Initialize the error term to negative half the X delta.
            error = (UInt16)(-deltaX / 2);

            // Determine the direction to step in the Y axis when required.
            if (y1 < y2) yStep = 1;
            else yStep = -1;

            // Loop through all the points along the X axis of the line.
            for (; x1 <= x2; x1++)
            {
                // See if this is a steep line.
                if (SwapXY)
                {
                    // Plot this point of the line, swapping the X and Y coordinates.
                    PixelDraw(display, y1, x1, ulValue);
                }
                else
                {
                    // Plot this point of the line, using the coordinates as is.
                    PixelDraw(display, x1, y1, ulValue);
                }
                // Increment the error term by the Y delta.
                error += deltaY;
                // See if the error term is now greater than zero.
                if (error > 0)
                {
                    // Take a step in the Y axis.
                    y1 = (UInt16)(y1 + yStep); // this could be a - or a + step
                    // Decrement the error term by the X delta.
                    error -= deltaX;
                }
            }
        }

        public static void LineDrawH(ILI9341Display display, UInt16 x, UInt16 y, UInt16 width, UInt16 color)
        {
            // Rudimentary clipping
            if ((x >= display.LCD_HORIZONTAL_MAX) || (y >= display.LCD_VERTICAL_MAX)) return;

            if ((x + width - 1) >= display.LCD_HORIZONTAL_MAX) width = (UInt16)(display.LCD_HORIZONTAL_MAX - x);
            // this should set a 1 row high line on the screen for writing to
            SetAddress(display, x, y, (UInt16)(x + width - 1), y);

            byte hi = (byte)(color >> 8);
            byte low = (byte)(color & 0xFF);
            byte[] tmparray = new byte[width * 2];
            //blast through creating the line
            for (int col = 0; col < tmparray.Length; col += 2)
            {
                tmparray[col + 1] = low;
                tmparray[col] = hi;
            }
            //send asap to the screen
            SendCommand(display, CMD_MEMORY_WRITE_MODE);
            SendData(display, tmparray);
        }

        public static void LineDrawV(ILI9341Display display, UInt16 x, UInt16 y, UInt16 height, UInt16 color)
        {

            // Rudimentary clipping
            if ((x >= display.LCD_HORIZONTAL_MAX) || (y >= display.LCD_VERTICAL_MAX)) return;

            if ((y + height - 1) >= display.LCD_VERTICAL_MAX) height = (UInt16)(display.LCD_VERTICAL_MAX - y);
            // this should set a 1 column wide line on the screen for writing to
            SetAddress(display, x, y, x, (UInt16)(y + height - 1));

            byte hi = (byte)(color >> 8);
            byte low = (byte)(color & 0xFF);
            byte[] tmparray = new byte[height * 2];
            //blast through creating the line
            for (int row = 0; row < tmparray.Length; row += 2)
            {
                tmparray[row] = hi;
                tmparray[row + 1] = low;
            }
            //send asap to the screen
            SendCommand(display, CMD_MEMORY_WRITE_MODE);
            SendData(display, tmparray);
        }

        public static void fillRect(ILI9341Display display, UInt16 x, UInt16 y, UInt16 width, UInt16 height, int color)
        {
            try
            {
                // Rudimentary clipping
                if ((x >= display.LCD_HORIZONTAL_MAX) || (y >= display.LCD_VERTICAL_MAX)) return;
                if ((x + width - 1) >= display.LCD_HORIZONTAL_MAX) width = (UInt16)(display.LCD_HORIZONTAL_MAX - x);
                if ((y + height - 1) >= display.LCD_VERTICAL_MAX) height = (UInt16)(display.LCD_VERTICAL_MAX - y);


                byte hi = (byte)(color >> 8), low = (byte)(color & 0xff);
                byte[] tmparray = new byte[height * width * 2];

                for (int xb = 0; xb < tmparray.Length; xb += 2)
                {
                    tmparray[xb + 1] = low;
                    tmparray[xb] = hi;
                }
                SetAddress(display, x, y, (UInt16)(x + width - 1), (UInt16)(y + height - 1));
                SendCommand(display, CMD_MEMORY_WRITE_MODE);
                SendData(display, tmparray);
            }
            catch (Exception ex)
            {

                throw;
            }


        }

        public static GpioPin InitGPIO(int GPIOpin, GpioPinDriveMode mode, GpioPinValue HiLow)
        {
            var gpio = GpioController.GetDefault();
            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                return null;
            }

            var pin = gpio.OpenPin(GPIOpin);

            if (pin == null)
            {
                return null;
            }
            pin.SetDriveMode(mode);
            pin.Write(HiLow);
            return pin;
        }

        public static async Task ResetDisplay(ILI9341Display display)
        {
            // assume power has just been turned on
            display.ResetPin.Write(GpioPinValue.High);
            await Task.Delay(10);
            display.ResetPin.Write(GpioPinValue.Low);   // reset
            await Task.Delay(10);                        // wait 10 ms
            display.ResetPin.Write(GpioPinValue.High);  // out of reset
            await Task.Delay(150);
        }

        public static async Task InitRegisters(ILI9341Display display)
        {
            // MORE PADDING
            SendCommand(display, CMD_POWER_CONTROL_1); SendData(display, new byte[] { padding, 0x10, padding, 0x10 });
            SendCommand(display, CMD_POWER_CONTROL_2); SendData(display, new byte[] { padding, 0x41 });
            SendCommand(display, CMD_VCOM_CONTROL_1); SendData(display, new byte[] { padding, 0x00, padding, 0x22, padding, 0x80 });
            SendCommand(display, CMD_MEMORY_ACCESS_CONTROL); SendData(display, new byte[] { padding, 0x68 });
            SendCommand(display, CMD_INTERFACE_MODE_CONTROL); SendData(display, new byte[] { padding, 0x00 });
            SendCommand(display, CMD_FRAME_RATE_CONTROL_NORMAL); SendData(display, new byte[] { padding, 0xB0, padding, 0x11 });
            SendCommand(display, CMD_DISPLAY_INVERSION_CONTROL); SendData(display, new byte[] { padding, 0x02 });
            SendCommand(display, CMD_INTERFACE_PIXEL_FORMAT); SendData(display, new byte[] { padding, 0x55 }); // the wave share is actually working in 16bit parallel mode
            SendCommand(display, CMD_SET_IMAGE_FUNCTION); SendData(display, new byte[] { padding, 0x01 });
            SendCommand(display, CMD_ENTRY_MODE_SET); SendData(display, new byte[] { padding, 0xC6 });
            SendCommand(display, CMD_ADJUST_CONTROL_3); SendData(display, new byte[] { padding, 0xA9, padding, 0x51, padding, 0x2C, padding, 0x82 });
            SendCommand(display, CMD_DISPLAY_FUNCTION_CONTROL);
            SendData(display, new byte[] { padding, 0x00, padding, 0x22, padding, 0x3B });
            //SendCommand(display, ILI9488_CMD_POSITIVE_GAMMA_CTRL); SendData(display, new byte[] { padding, 0x00, padding, 0x07, padding, 0x0F, padding, 0x0D, padding, 0x1B, padding, 0x0A, padding, 0x3C, padding, 0x78, padding, 0x4A, padding, 0x07, padding, 0x0E, padding, 0x09, padding, 0x1B, padding, 0x1E, padding, 0x0F });
            //SendCommand(display, ILI9488_CMD_NEGATIVE_GAMMA_CTRL); SendData(display, new byte[] { padding, 0x00, padding, 0x22, padding, 0x24, padding, 0x06, padding, 0x12, padding, 0x07, padding, 0x36, padding, 0x47, padding, 0x47, padding, 0x06, padding, 0x0A, padding, 0x07, padding, 0x30, padding, 0x37, padding, 0x0F });
            SendCommand(display, CMD_POSITIVE_GAMMA_CTRL);
            SendData(display, new byte[] { padding, 0x0F, padding, 0x1F, padding, 0x1C, padding, 0x0B, padding, 0x0E, padding, 0x09, padding, 0x48, padding, 0x99, padding, 0x38, padding, 0x0A, padding, 0x14, padding, 0x06, padding, 0x11, padding, 0x09, padding, 0x00 });
            SendCommand(display, CMD_NEGATIVE_GAMMA_CTRL);
            SendData(display, new byte[] { padding, 0x0F, padding, 0x36, padding, 0x2E, padding, 0x09, padding, 0x0A, padding, 0x04, padding, 0x46, padding, 0x66, padding, 0x37, padding, 0x06, padding, 0x10, padding, 0x04, padding, 0x24, padding, 0x20, padding, 0x00 });
        }

        public static async Task InitILI9341DisplaySPI(ILI9341Display display, int SPIDisplaypin, int speed, SpiMode mode, string SPI_CONTROLLER_NAME, string DefaultDisplay)
        {
            var displaySettings = new SpiConnectionSettings(SPIDisplaypin);
            displaySettings.ClockFrequency = speed;// 500kHz;
            displaySettings.Mode = mode; //Mode0,1,2,3;  MCP23S17 needs mode 0
            string DispspiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
            var DispdeviceInfo = await DeviceInformation.FindAllAsync(DispspiAqs);
            display.SpiDisplay = await SpiDevice.FromIdAsync(DispdeviceInfo[0].Id, displaySettings);

            //await ResetDisplay(display);
            //await InitRegisters(display);
            //await Wakeup(display);

            if (String.IsNullOrEmpty(DefaultDisplay))
                InitializeDisplayBuffer(display, ILI9341Display.BLACK);
            else
            {
                await LoadBitmap(display, DefaultDisplay);
            }
            await PowerOnSequence(display);
            await Wakeup(display);
            Flush(display);


        }

        public static async Task PowerOnSequence(ILI9341Display display)
        {
            // assume power has just been turned on
            await Task.Delay(5);
            display.ResetPin.Write(GpioPinValue.Low);   // reset
            await Task.Delay(5);                        // wait 5 ms
            display.ResetPin.Write(GpioPinValue.High);  // out of reset
            await Task.Delay(20);
        }

        //public static void BacklightOff(ILI9341Display display)
        //{

        //    SendCommand(display, CMD_DISPLAY_FUNCTION_CONTROL);

        //    SendData(display, new byte[] { 0x0A, 0x02 });
        //}

        public static async Task Wakeup(ILI9341Display display)
        {
            SendCommand(display, CMD_SLEEP_OUT);
            await Task.Delay(60);

            SendCommand(display, CMD_POWER_CONTROL_A); SendData(display, new byte[] { 0x39, 0x2C, 0x00, 0x34, 0x02 });
            SendCommand(display, CMD_POWER_CONTROL_B); SendData(display, new byte[] { 0x00, 0xC1, 0x30 });
            SendCommand(display, CMD_DRIVER_TIMING_CONTROL_A); SendData(display, new byte[] { 0x85, 0x00, 0x78 });
            SendCommand(display, CMD_DRIVER_TIMING_CONTROL_B); SendData(display, new byte[] { 0x00, 0x00 });

            SendCommand(display, CMD_POWER_ON_SEQUENCE_CONTROL); SendData(display, new byte[] { 0x64, 0x03, 0x12, 0x81 });
            SendCommand(display, CMD_PUMP_RATIO_CONTROL); SendData(display, new byte[] { 0x20 });

            SendCommand(display, CMD_POWER_CONTROL_1); SendData(display, new byte[] { 0x23 });
            SendCommand(display, CMD_POWER_CONTROL_2); SendData(display, new byte[] { 0x10 });

            SendCommand(display, CMD_VCOM_CONTROL_1); SendData(display, new byte[] { 0x3e, 0x28 });
            SendCommand(display, CMD_VCOM_CONTROL_2); SendData(display, new byte[] { 0x86 });

            SendCommand(display, CMD_MEMORY_ACCESS_CONTROL); SendData(display, new byte[] { 0x48 });
            SendCommand(display, CMD_PIXEL_FORMAT); SendData(display, new byte[] { 0x55 });
            SendCommand(display, CMD_FRAME_RATE_CONTROL); SendData(display, new byte[] { 0x00, 0x18 });
            SendCommand(display, CMD_DISPLAY_FUNCTION_CONTROL); SendData(display, new byte[] { 0x08, 0x82, 0x27 });
            SendCommand(display, CMD_ENABLE_3G); SendData(display, new byte[] { 0x00 });
            SendCommand(display, CMD_GAMMA_SET); SendData(display, new byte[] { 0x01 });
            SendCommand(display, CMD_POSITIVE_GAMMA_CORRECTION); SendData(display, new byte[] { 0x0F, 0x31, 0x2B, 0x0C, 0x0E, 0x08, 0x4E, 0xF1, 0x37, 0x07, 0x10, 0x03, 0x0E, 0x09, 0x00 });
            SendCommand(display, CMD_NEGATIVE_GAMMA_CORRECTION); SendData(display, new byte[] { 0x00, 0x0E, 0x14, 0x03, 0x11, 0x07, 0x31, 0xC1, 0x48, 0x08, 0x0F, 0x0C, 0x31, 0x36, 0x0F });

            SendCommand(display, CMD_SLEEP_OUT);
            await Task.Delay(120);
            SendCommand(display, CMD_DISPLAY_ON);
        }

        public async static void Sleep(ILI9341Display display)
        {
            SendCommand(display, CMD_DISPLAY_OFF);
            await Task.Delay(TimeSpan.FromSeconds(1));
            SendCommand(display, CMD_ENTER_SLEEP);
        }

        public static void DisplayOff(ILI9341Display display)
        {
            SendCommand(display, CMD_DISPLAY_OFF);
        }

        //public static void DisplayOn(ILI9341Display display)
        //{
        //    SendCommand(display, CMD_DISPLAY_ON);
        //}

        public static void CleanUp()
        {
            //SpiGPIO.Dispose();
            //ResetPin.Dispose();
            //DataCommandPin.Dispose();
        }
        public static async Task landscapeMode(ILI9341Display display)
        {
            try
            {
                SendCommand(display, CMD_MEMORY_ACCESS_CONTROL); SendData(display, new byte[] { 0xE8 });
                display.LCD_HORIZONTAL_MAX = ILI9341Display.HORIZONTAL_MAX_DEFAULT;
                display.LCD_VERTICAL_MAX = ILI9341Display.VERTICAL_MAX_DEFAULT;
                await LoadBitmap(display, display.currentImage);
                Flush(display);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static async Task PortrateMode(ILI9341Display display)
        {
            try
            {
                SendCommand(display, CMD_MEMORY_ACCESS_CONTROL); SendData(display, new byte[] { 0x48 });
                display.LCD_VERTICAL_MAX = ILI9341Display.VERTICAL_MAX_DEFAULT;
                display.LCD_HORIZONTAL_MAX = ILI9341Display.HORIZONTAL_MAX_DEFAULT;
                await LoadBitmap(display, display.currentImage);
                Flush(display);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private static void SetAddress(ILI9341Display display, UInt16 x0, UInt16 y0, UInt16 x1, UInt16 y1)
        {
            SendCommand(display, CMD_COLUMN_ADDRESS_SET);
            SendData(display, new byte[] { (byte)(x0 >> 8), (byte)(x0), (byte)(x1 >> 8), (byte)(x1) });
            SendCommand(display, CMD_PAGE_ADDRESS_SET);
            SendData(display, new byte[] { (byte)(y0 >> 8), (byte)(y0), (byte)(y1 >> 8), (byte)(y1) });
        }

        public static ushort RGB24ToRGB565(byte Red, byte Green, byte Blue)
        {
            UInt16 red565 = (UInt16)((Red * 249 + 1014) >> 11);
            UInt16 green565 = (UInt16)((Green * 253 + 505) >> 10);
            UInt16 blue565 = (UInt16)((Blue * 249 + 1014) >> 11);
            return (UInt16)(red565 << 11 | green565 << 5 | blue565);
        }

        public static void InitializeDisplayBuffer(ILI9341Display display, UInt16 colour)
        {
            for (uint i = 0; i < display.LCD_HORIZONTAL_MAX * display.LCD_VERTICAL_MAX; i++)
            {
                display.DisplayBuffer[i * 2] = (byte)(colour >> 8);
                display.DisplayBuffer[i * 2 + 1] = (byte)(colour & 0xFF);
            }
        }

        // loads a bitmap directly to the screen area designated by the x1, y1, width and height, it will scale etc automatically
        public static async Task LoadBitmap(ILI9341Display display, UInt16 x1, UInt16 y1, UInt16 width, UInt16 height, string name)
        {
            // basic bounds checking
            if ((x1 + width > display.LCD_HORIZONTAL_MAX) | (y1 + height > display.LCD_VERTICAL_MAX)) return;
            byte[] imagemap;
            try
            {
                StorageFile srcfile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(name));

                using (IRandomAccessStream fileStream = await srcfile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                    BitmapTransform transform = new BitmapTransform()
                    {
                        ScaledWidth = Convert.ToUInt32(width),
                        ScaledHeight = Convert.ToUInt32(height)
                    };
                    PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.DoNotColorManage
                    );
                    byte[] sourcePixels = pixelData.DetachPixelData();
                    if (sourcePixels.Length != width * height * 4) return; // make sure it scaled and loaded right
                    imagemap = new byte[width * height * 2]; // something to put the image in
                    int pi = 0;
                    for (UInt32 x = 0; x < sourcePixels.Length - 1; x += 4)
                    {
                        // we ignore the alpha value [3]
                        ushort temp = RGB24ToRGB565(sourcePixels[x + 2], sourcePixels[x + 1], sourcePixels[x]);
                        imagemap[pi * 2] = (byte)((temp >> 8) & 0xFF);
                        imagemap[pi * 2 + 1] = (byte)(temp & 0xFF);
                        pi++;
                    }
                }
                SetAddress(display, x1, y1, (UInt16)(x1 + width - 1), (UInt16)(y1 + height - 1));
                SendCommand(display, CMD_MEMORY_WRITE_MODE);
                SendData(display, imagemap);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task LoadBitmap(ILI9341Display display, string name)
        {
            try
            {
                StorageFile srcfile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(name));

                using (IRandomAccessStream fileStream = await srcfile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                    BitmapTransform transform = new BitmapTransform()
                    {
                        ScaledWidth = Convert.ToUInt32(display.LCD_HORIZONTAL_MAX),
                        ScaledHeight = Convert.ToUInt32(display.LCD_VERTICAL_MAX)
                    };
                    PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.DoNotColorManage
                    );
                    byte[] sourcePixels = pixelData.DetachPixelData();
                    if (sourcePixels.Length != display.LCD_HORIZONTAL_MAX * display.LCD_VERTICAL_MAX * 4)
                        return;
                    int pi = 0;
                    for (UInt32 x = 0; x < sourcePixels.Length - 1; x += 4)
                    {
                        // we ignore the alpha value [3]
                        ushort temp = ILI9341.RGB24ToRGB565(sourcePixels[x + 2], sourcePixels[x + 1], sourcePixels[x]);
                        display.DisplayBuffer[pi * 2] = (byte)((temp >> 8) & 0xFF);
                        display.DisplayBuffer[pi * 2 + 1] = (byte)(temp & 0xFF);
                        pi++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            display.currentImage = name;
        }

        public static void Flush(ILI9341Display display)
        {
            if (display.DisplayBuffer.Length != display.LCD_VERTICAL_MAX * display.LCD_HORIZONTAL_MAX * 2) return;
            SetAddress(display, 0, 0, (UInt16)(display.LCD_HORIZONTAL_MAX - 1), (UInt16)(display.LCD_VERTICAL_MAX - 1));
            int block_size = 51200; // limits of the SPI interface is 64K but this is an even block for display ???
            int numBlocks = display.DisplayBuffer.Length / block_size;
            byte[] buffer = new byte[block_size];
            // now we start to write the buffer out
            SendCommand(display, CMD_MEMORY_WRITE_MODE);

            for (int block = 0; block < numBlocks; block++)
            {
                Array.Copy(display.DisplayBuffer, block * block_size, buffer, 0, block_size);
                SendData(display, buffer);
            }

           
        }

        private static void SendData(ILI9341Display display, byte[] Data)
        {
            display.DCPin.Write(GpioPinValue.High);
            int blocksize = 65536;
            if (Data.Length > blocksize)
            {
                int numBlocks = Data.Length / blocksize;
                byte[] buffer = new byte[blocksize];
                for (int block = 0; block < numBlocks; block++)
                {
                    Array.Copy(Data, block * blocksize, buffer, 0, blocksize);
                    display.SpiDisplay.Write(buffer);
                }
                if (numBlocks * numBlocks < Data.Length)
                {
                    // we have a bit more to send
                    byte[] bufferlast = new byte[Data.Length - (numBlocks * blocksize)];
                    Array.Copy(Data, Data.Length - bufferlast.Length, bufferlast, 0, bufferlast.Length);
                    display.SpiDisplay.Write(bufferlast);
                }
            }
            else
            {
                display.SpiDisplay.Write(Data);
            }
        }

        private static void SendCommand(ILI9341Display display, byte[] Command)
        {
            display.DCPin.Write(GpioPinValue.Low);
            display.SpiDisplay.Write(Command);
        }
    }
}
