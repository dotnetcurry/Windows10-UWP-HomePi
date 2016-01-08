using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using System.IO;
using Windows.Storage;

namespace HomePi
{

    //calibration points for touchpanel
    public class CAL_POINT
    {
        public int x;
        public int y;
    };

    //calibration matrix for touchpanel
    public class CAL_MATRIX
    {
        public DateTime LastUpdated;
        public int a;
        public int b;
        public int c;
        public int d;
        public int e;
        public int f;
        public int div;
        public string message;
        public CAL_MATRIX()
        {
            LastUpdated = DateTime.MaxValue;
            a = 0;
            b = 0;
            c = 0;
            d = 0;
            e = 0;
            f = 0;
            div = 0;
        }
        public CAL_MATRIX(DateTime cal_LastUpdated, int cal_a, int cal_b, int cal_c, int cal_d, int cal_e, int cal_f, int cal_div, string cal_message)
        {
            LastUpdated = cal_LastUpdated;
            a = cal_a;
            b = cal_b;
            c = cal_c;
            d = cal_d;
            e = cal_e;
            f = cal_f;
            div = cal_div;
            message = cal_message;
        }
        // read an array of Cal parameters from a given file name
        public async Task<bool> SaveCalData(string fileName)
        {
            // We change file extension here to make sure it's a .csv file.
            if (div == 0) return false; // cal data does not valid
            string[] lines = { LastUpdated.ToString(), a.ToString(), b.ToString(), c.ToString(), d.ToString(), e.ToString(), f.ToString(), div.ToString() };
            try
            {

                var storageFolder = KnownFolders.DocumentsLibrary;
                var file = await storageFolder.CreateFileAsync(Path.ChangeExtension(fileName, ".cal"), CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, String.Join(",", lines));
                message = "Sucessfully saved Calibration Data";
                //File.WriteAllLines(System.IO.Path.ChangeExtension(fileName, ".cal"), lines);
                return true; // sucess
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
        public async Task<bool> LoadCalData(string fileName)
        {
            // We change file extension here to make sure it's a .csv file.
            try
            {
                StorageFolder storageFolder = KnownFolders.DocumentsLibrary;
                StorageFile calFile = await storageFolder.GetFileAsync(System.IO.Path.ChangeExtension(fileName, ".cal"));
                string CalData = await FileIO.ReadTextAsync(calFile);
                message = "Sucessfully saved Calibration Data";
                string[] data = CalData.Split(new char[] { ',' });
                // We return a calibration data file with the data in order.
                LastUpdated = Convert.ToDateTime(data[0]);
                a = Convert.ToInt32(data[1]);
                b = Convert.ToInt32(data[2]);
                c = Convert.ToInt32(data[3]);
                d = Convert.ToInt32(data[4]);
                e = Convert.ToInt32(data[5]);
                f = Convert.ToInt32(data[6]);
                div = Convert.ToInt32(data[7]);
                message = (div == 0) ? "Good data" : "Bad Data";
                return true;
            }
            catch (Exception ex)
            {
                LastUpdated = DateTime.MaxValue;
                a = 0;
                b = 0;
                c = 0;
                d = 0;
                e = 0;
                f = 0;
                div = 0;
                message = ex.Message;
                return false;
            }
        }
    };
    public static class TSC2046
    {
        public static int lcd_orientation;      //lcd_orientation
        public static int lcd_x, lcd_y;         //calibrated pos (screen)
        public static int tp_x, tp_y;           //raw pos (touch panel)
        public static int tp_last_x, tp_last_y; //last raw pos (touch panel)
        public static CAL_MATRIX CalibrationMatrix = new CAL_MATRIX();        //calibrate matrix
        public static int pressure;             //touch panel pressure
        public static int MIN_PRESSURE = 15;     //minimum pressure 1...254
        public static int LCD_WIDTH = 480;
        public static int LCD_HEIGHT = 320;
        public static int CS_PIN = 1;

        public static byte CMD_START = 0x80;
        public static byte CMD_12BIT = 0x00;
        public static byte CMD_8BIT = 0x08;
        public static byte CMD_DIFF = 0x00;
        public static byte CMD_SINGLE = 0x04;
        public static byte CMD_X_POS = 0x10;
        public static byte CMD_Z1_POS = 0x30;
        public static byte CMD_Z2_POS = 0x40;
        public static byte CMD_Y_POS = 0x50;
        public static byte CMD_PWD = 0x00;
        public static byte CMD_ALWAYSON = 0x03;

        public static SpiDevice touchSPI;

        public static async Task InitTSC2046SPI()
        {
            var touchSettings = new SpiConnectionSettings(CS_PIN);
            touchSettings.ClockFrequency = 125000;
            touchSettings.Mode = SpiMode.Mode0; //Mode0,1,2,3;  MCP23S17 needs mode 0
            string DispspiAqs = SpiDevice.GetDeviceSelector("SPI0");
            var DispdeviceInfo = await DeviceInformation.FindAllAsync(DispspiAqs);
            touchSPI = await SpiDevice.FromIdAsync(DispdeviceInfo[0].Id, touchSettings);
            //set vars
            CalibrationMatrix.div = 0;
            tp_x = 0;
            tp_y = 0;
            tp_last_x = 0;
            tp_last_y = 0;
            lcd_x = 0;
            lcd_y = 0;
            pressure = 0;
            setOrientation(0);
        }
        public static void setOrientation(UInt16 Orientation)
        {
            switch (Orientation)
            {
                default:
                case 0:
                    lcd_orientation = 0;
                    break;
                case 90:
                    lcd_orientation = 90;
                    break;
                case 180:
                    lcd_orientation = 180;
                    break;
                case 270:
                    lcd_orientation = 270;
                    break;
            }
        }
        public static void setRotation(UInt16 r)
        {
            setOrientation(r);
        }
        public static int setCalibration(CAL_POINT[] lcd, CAL_POINT[] tp)
        {
            CalibrationMatrix.div = ((tp[0].x - tp[2].x) * (tp[1].y - tp[2].y)) -
                            ((tp[1].x - tp[2].x) * (tp[0].y - tp[2].y));

            if (CalibrationMatrix.div == 0)
            {
                return 0;
            }

            CalibrationMatrix.a = ((lcd[0].x - lcd[2].x) * (tp[1].y - tp[2].y)) -
                          ((lcd[1].x - lcd[2].x) * (tp[0].y - tp[2].y));

            CalibrationMatrix.b = ((tp[0].x - tp[2].x) * (lcd[1].x - lcd[2].x)) -
                          ((lcd[0].x - lcd[2].x) * (tp[1].x - tp[2].x));

            CalibrationMatrix.c = (tp[2].x * lcd[1].x - tp[1].x * lcd[2].x) * tp[0].y +
                          (tp[0].x * lcd[2].x - tp[2].x * lcd[0].x) * tp[1].y +
                          (tp[1].x * lcd[0].x - tp[0].x * lcd[1].x) * tp[2].y;

            CalibrationMatrix.d = ((lcd[0].y - lcd[2].y) * (tp[1].y - tp[2].y)) -
                          ((lcd[1].y - lcd[2].y) * (tp[0].y - tp[2].y));

            CalibrationMatrix.e = ((tp[0].x - tp[2].x) * (lcd[1].y - lcd[2].y)) -
                          ((lcd[0].y - lcd[2].y) * (tp[1].x - tp[2].x));

            CalibrationMatrix.f = (tp[2].x * lcd[1].y - tp[1].x * lcd[2].y) * tp[0].y +
                          (tp[0].x * lcd[2].y - tp[2].x * lcd[0].y) * tp[1].y +
                          (tp[1].x * lcd[0].y - tp[0].x * lcd[1].y) * tp[2].y;

            CalibrationMatrix.LastUpdated = DateTime.Now;
            return 1;
        }
        public static void TouchToDispAdjust()
        {
            if (CalibrationMatrix.div == 0) return; // we have not calibrated the PEN
            int x = 0, y = 0;

            //calc x pos
            if (tp_x != tp_last_x)
            {
                tp_last_x = tp_x;
                x = tp_x;
                x = ((CalibrationMatrix.a * x) + (CalibrationMatrix.b * y) + CalibrationMatrix.c) / CalibrationMatrix.div;
                if (x < 0) { x = 0; }
                else if (x >= LCD_WIDTH) { x = LCD_WIDTH - 1; }
                lcd_x = x;
            }
            //calc y pos
            if (tp_y != tp_last_y)
            {
                tp_last_y = tp_y;
                y = tp_y;
                y = ((CalibrationMatrix.d * x) + (CalibrationMatrix.e * y) + CalibrationMatrix.f) / CalibrationMatrix.div;
                if (y < 0) { y = 0; }
                else if (y >= LCD_HEIGHT) { y = LCD_HEIGHT - 1; }
                lcd_y = y;
            }
        }
        public static int getDispX()
        {
            TouchToDispAdjust();
            switch (lcd_orientation)
            {
                case 0: return lcd_x;
                case 90: return lcd_y;
                case 180: return LCD_WIDTH - lcd_x;
                case 270: return LCD_HEIGHT - lcd_y;
            }
            return 0;
        }
        public static int getDispY()
        {
            TouchToDispAdjust();
            switch (lcd_orientation)
            {
                case 0: return lcd_y;
                case 90: return LCD_WIDTH - lcd_x;
                case 180: return LCD_HEIGHT - lcd_y;
                case 270: return lcd_x;
            }
            return 0;
        }
        public static int getTouchX()
        {
            return tp_x;
        }
        public static int getTouchY()
        {
            return tp_y;
        }
        public static int getPressure()
        {
            return pressure;
        }
        public static void CheckTouch()
        {
            try
            {
                int p, a1, a2, b1, b2;
                int x, y;
                byte[] writeBuffer24 = new byte[3];
                byte[] readBuffer24 = new byte[3];

                //get pressure first to see if the screen is being touched
                writeBuffer24[0] = (byte)(CMD_START | CMD_8BIT | CMD_DIFF | CMD_Z1_POS);
                touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                a1 = readBuffer24[1] & 0x7F;

                writeBuffer24[0] = (byte)(CMD_START | CMD_8BIT | CMD_DIFF | CMD_Z2_POS);
                touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                b1 = 255 - readBuffer24[1] & 0x7F;
                p = a1 + b1;

                if (p > MIN_PRESSURE)
                {
                    //using 2 samples for x and y position
                    //get X data
                    writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_X_POS);
                    touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                    a1 = readBuffer24[1];
                    b1 = readBuffer24[2];
                    writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_X_POS);
                    touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                    a2 = readBuffer24[1];
                    b2 = readBuffer24[2];

                    if (a1 == a2)
                    {
                        x = ((a2 << 4) | (b2 >> 4)); //12bit: ((a<<4)|(b>>4)) //10bit: ((a<<2)|(b>>6))

                        //get Y data
                        writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_Y_POS);
                        touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                        a1 = readBuffer24[1];
                        b1 = readBuffer24[2];
                        writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_Y_POS);
                        touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                        a2 = readBuffer24[1];
                        b2 = readBuffer24[2];

                        if (a1 == a2)
                        {
                            y = ((a2 << 4) | (b2 >> 4)); //12bit: ((a<<4)|(b>>4)) //10bit: ((a<<2)|(b>>6))
                            if (x > 0 && y > 0)
                            {
                                tp_x = x;
                                tp_y = y;
                            }
                            pressure = p;
                        }
                    }
                }
                else
                {
                    pressure = 0;
                }

            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
    } // end class
} // end namespace
