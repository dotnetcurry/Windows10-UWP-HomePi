using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HomePi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string tempC = string.Empty;
        private DispatcherTimer weathertimer;        
        const string CalibrationFilename = "TSC2046";
        private Point lastPosition = new Point(double.NaN, double.NaN);

        const Int32 DCPIN = 22;
        const Int32 RESETPIN = 27;

        ILI9341Display display1 = new ILI9341Display(ILI9341Display.BLACK, DCPIN, RESETPIN);

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Init();
            base.OnNavigatedTo(e);
        }
        
        private void initTimer()
        {
            weathertimer = new DispatcherTimer();
            weathertimer.Interval = TimeSpan.FromHours(1); 
            weathertimer.Tick += Timer_Tick;
            weathertimer.Start();
        }

        private async void Timer_Tick(object sender, object e)
        {
            GetWeather();
        }

        private async Task loadImageToScreen(string imageName)
        {
            try
            {
                await ILI9341.LoadBitmap(display1, imageName);
            }
            catch (Exception ex)
            {
                //TODO : Handle error
            }
        }

        private async void CalibrateTouch()
        {
            //5 point calibration
            CAL_POINT[] touchPoints = new CAL_POINT[5];
            touchPoints[0] = new CAL_POINT();
            touchPoints[1] = new CAL_POINT();
            touchPoints[2] = new CAL_POINT();
            touchPoints[3] = new CAL_POINT();
            touchPoints[4] = new CAL_POINT();

            CAL_POINT[] screenPoints = new CAL_POINT[5];
            screenPoints[0] = new CAL_POINT();
            screenPoints[1] = new CAL_POINT();
            screenPoints[2] = new CAL_POINT();
            screenPoints[3] = new CAL_POINT();
            screenPoints[4] = new CAL_POINT();

            ILI9341.fillRect(display1, 0, 0, 240, 320, 0x0000);

            ILI9341.LineDrawH(display1, 30, 30, 1, 0xFFFF);
            ILI9341.LineDrawV(display1, 30, 30, 1, 0xFFFF);
            while (TSC2046.pressure < 5) { TSC2046.CheckTouch(); }//wait for pen pressure
            screenPoints[0].x = 30;
            screenPoints[0].y = 30;
            touchPoints[0].x = TSC2046.tp_x;
            touchPoints[0].y = TSC2046.tp_y;
            while (TSC2046.pressure > 1) { TSC2046.CheckTouch(); } // wait for release of pen


            ILI9341.LineDrawH(display1, 30, 300, 1, 0xFFFF);
            ILI9341.LineDrawV(display1, 30, 300, 1, 0xFFFF);
            while (TSC2046.pressure < 5) { TSC2046.CheckTouch(); }//wait for pen pressure
            screenPoints[1].x = 300;
            screenPoints[1].y = 30;
            touchPoints[1].x = TSC2046.tp_x;
            touchPoints[1].y = TSC2046.tp_y;
            while (TSC2046.pressure > 1) { TSC2046.CheckTouch(); }// wait for release of pen

            ILI9341.LineDrawH(display1, 120, 160, 1, 0xFFFF);
            ILI9341.LineDrawV(display1, 120, 160, 1, 0xFFFF);
            while (TSC2046.pressure < 5) { TSC2046.CheckTouch(); }//wait for pen pressure
            screenPoints[2].x = 160;
            screenPoints[2].y = 120;
            touchPoints[2].x = TSC2046.tp_x;
            touchPoints[2].y = TSC2046.tp_y;
            while (TSC2046.pressure > 1) { TSC2046.CheckTouch(); }// wait for release of pen


            ILI9341.LineDrawH(display1, 210, 30, 1, 0xFFFF);
            ILI9341.LineDrawV(display1, 210, 30, 1, 0xFFFF);
            while (TSC2046.pressure < 5) { TSC2046.CheckTouch(); }//wait for pen pressure
            screenPoints[3].x = 30;
            screenPoints[3].y = 210;
            touchPoints[3].x = TSC2046.tp_x;
            touchPoints[3].y = TSC2046.tp_y;
            while (TSC2046.pressure > 1) { TSC2046.CheckTouch(); }// wait for release of pen


            ILI9341.LineDrawH(display1, 210, 300, 1, 0xFFFF);
            ILI9341.LineDrawV(display1, 210, 300, 1, 0xFFFF);
            while (TSC2046.pressure < 5) { TSC2046.CheckTouch(); }//wait for pen pressure
            screenPoints[4].x = 300;
            screenPoints[4].y = 210;
            touchPoints[4].x = TSC2046.tp_x;
            touchPoints[4].y = TSC2046.tp_y;
            while (TSC2046.pressure > 1) { TSC2046.CheckTouch(); }// wait for release of pen

            TSC2046.setCalibration(screenPoints, touchPoints);
            if (await TSC2046.CalibrationMatrix.SaveCalData(CalibrationFilename))
            {
                //Success
            }
            else
            {
                //Handle error
            }
            ILI9341.Flush(display1);
        }

        private async void GetWeather()
        {
            ILI9341.Flush(display1);
            //paint Menu
            await ILI9341.LoadBitmap(display1, 0, 0, 240, 320, "ms-appx:///assets/Home.png");

            //Write loading
            ILI9341.setCursor(display1, 60, 120);
            ILI9341.write(display1, "Checking...".ToCharArray(), 2, 0x86D2);

            //Get Weather
            string responseText = await GetjsonStream("http://api.worldweatheronline.com/free/v2/weather.ashx?q=Vienna, Austria&format=JSON&extra=&num_of_days=2&date=&fx=&cc=&includelocation=&show_comments=&callback=&key=4b3c8aaa970207d3e4a5c7fb6d514");
            LocalWeather localWeather = JsonConvert.DeserializeObject<LocalWeather>(responseText);

            tempC = string.Empty;
            tempC = localWeather.data.current_Condition[0].temp_C.ToString();

            //Clear
            ILI9341.fillRect(display1, 0, 0, 240, 150, 0xFFFF);

            //Get weather icon
            XElement iconXml = XElement.Load("wwoConditionCodes.xml");
            var iconditions = iconXml.Elements("condition");
            var iconCodeElement = iconditions.Where(i => i.Element("code").Value == localWeather.data.current_Condition[0].weatherCode).FirstOrDefault();

            string iconFile = string.Empty;
            if (DateTime.Now.Hour < 18)
            {
                iconFile = iconCodeElement.Element("day_icon").Value;
            }
            else
            {
                iconFile = iconCodeElement.Element("night_icon").Value;
            }

            iconFile = "ms-appx:///assets/Weather Icons/" + iconFile.Trim() + ".png";

            //Display Icon
            await ILI9341.LoadBitmap(display1, 60, 20, 120, 120, iconFile);

            if (Convert.ToInt16(tempC) > 9)
            {
                ILI9341.setCursor(display1, 80, 150);
            }
            else
            {
                ILI9341.setCursor(display1, 100, 150);
            }
            ILI9341.write(display1, tempC.ToCharArray(), 8, 0xE2C9);

            //Second row can display 20 characters
            //Calculate spacing

            int count = localWeather.data.current_Condition[0].weatherDesc[0].value.ToString().Length;

            string weatherDesc = localWeather.data.current_Condition[0].weatherDesc[0].value.ToString();
            UInt16 spacing = 12;
            if (weatherDesc.Length > 18)
            {
                weatherDesc = weatherDesc.Substring(0, 15) + "...";
            }
            else
            {
                int counter = ((18 - weatherDesc.Length) / 2) + 1;
                spacing += (UInt16)(spacing * counter);
            }

            ILI9341.setCursor(display1, spacing, 230);
            ILI9341.write(display1, weatherDesc.ToCharArray(), 2, 0xE2C9);

        }


        private async void Init()
        {
            await ILI9341.InitILI9341DisplaySPI(display1, 0, 50000000, SpiMode.Mode0, "SPI0", "");
            await TSC2046.InitTSC2046SPI();
            initTimer();

            if (!await TSC2046.CalibrationMatrix.LoadCalData(CalibrationFilename))
            {
                CalibrateTouch();
            }

            //Paint background
            ILI9341.fillRect(display1, 0, 0, 240, 320, 0xFFFF);

            GetWeather();
        }

        public async Task<string> GetjsonStream(string url) //Function to read from given url
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            HttpResponseMessage v = new HttpResponseMessage();
            return await response.Content.ReadAsStringAsync();
        }

    }
}

