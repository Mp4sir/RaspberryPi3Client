using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Devices.I2c;
using System.Threading;
using System.Diagnostics;

using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MCP9808
{
    public sealed partial class MainPage : Page
    {
        private const byte Address1 = 0x18;         // 7-bit I2C Address of the MCP9808 sensor
        private const byte Address2 = 0x19;
        private const byte Address3 = 0x1A;
        private const byte Address4 = 0x1B;
        private const byte Address5 = 0x1C;
        private const byte Address6 = 0x1E;

        private const byte RegReadAddress = 0x05;   // Address of the temperature data register

        private I2cDevice Sensor1;
        private I2cDevice Sensor2;
        private I2cDevice Sensor3;
        private I2cDevice Sensor4;
        private I2cDevice Sensor5;
        private I2cDevice Sensor6;

        private Timer periodicTimer;

        private string Sensor1Text;
        private string Sensor2Text;
        private string Sensor3Text;
        private string Sensor4Text;
        private string Sensor5Text;
        private string Sensor6Text;

        public double sensorData1 = 0;
        public double sensorData2 = 0;
        public double sensorData3 = 0;
        public double sensorData4 = 0;
        public double sensorData5 = 0;
        public double sensorData6 = 0;

        public MainPage()
        {
            this.InitializeComponent();

            Unloaded += MainPage_Unloaded;

            InitI2CTemp();
        }

        private async void InitI2CTemp()
        {
            // Initiate Sensor 1
            var settings1 = new I2cConnectionSettings(Address1);
            settings1.BusSpeed = I2cBusSpeed.FastMode;
            var controller1 = await I2cController.GetDefaultAsync();
            Sensor1 = controller1.GetDevice(settings1);

            // Initiate Sensor 2
            var settings2 = new I2cConnectionSettings(Address2);
            settings2.BusSpeed = I2cBusSpeed.FastMode;
            var controller2 = await I2cController.GetDefaultAsync();
            Sensor2 = controller2.GetDevice(settings2);

            // Initiate Sensor 3
            var settings3 = new I2cConnectionSettings(Address3);
            settings3.BusSpeed = I2cBusSpeed.FastMode;
            var controller3 = await I2cController.GetDefaultAsync();
            Sensor3 = controller3.GetDevice(settings3);

            // Initiate Sensor 4
            var settings4 = new I2cConnectionSettings(Address4);
            settings4.BusSpeed = I2cBusSpeed.FastMode;
            var controller4 = await I2cController.GetDefaultAsync();
            Sensor4 = controller4.GetDevice(settings4);

            // Initiate Sensor 5
            var settings5 = new I2cConnectionSettings(Address5);
            settings5.BusSpeed = I2cBusSpeed.FastMode;
            var controller5 = await I2cController.GetDefaultAsync();
            Sensor5 = controller5.GetDevice(settings5);

            // Initiate Sensor 6
            var settings6 = new I2cConnectionSettings(Address6);
            settings6.BusSpeed = I2cBusSpeed.FastMode;
            var controller6 = await I2cController.GetDefaultAsync();
            Sensor6 = controller6.GetDevice(settings6);

            periodicTimer = new Timer(this.TimerCallback, null, 0, 1000);   // Initiate Timer to read data every 1000ms
        }

        private void TimerCallback(object state)
        {

            try
            {
                sensorData1 = ReadI2CTemp(Sensor1);                               // Read Sensor 1
                Sensor1Text = String.Format("Sensor 1: {0:F3} C", sensorData1);   // Save temperature into Text

            }
            catch (Exception ex)
            {
                Sensor1Text = "Sensor 1: Not Found";
            }

            try
            {
                sensorData2 = ReadI2CTemp(Sensor2);                               // Read Sensor 2
                Sensor2Text = String.Format("Sensor 2: {0:F3} C", sensorData2);   // Save temperature into Text
            }
            catch (Exception ex)
            {
                Sensor2Text = "Sensor 2: Not Found";
            }

            try
            {
                sensorData3 = ReadI2CTemp(Sensor3);                               // Read Sensor 3
                Sensor3Text = String.Format("Sensor 3: {0:F3} C", sensorData3);   // Save temperature into Text

            }
            catch (Exception ex)
            {
                Sensor3Text = "Sensor 3: Not Found";
            }

            try
            {
                sensorData4 = ReadI2CTemp(Sensor4);                               // Read Sensor 4
                Sensor4Text = String.Format("Sensor 4: {0:F3} C", sensorData4);   // Save temperature into Text
            }
            catch (Exception ex)
            {
                Sensor4Text = "Sensor 4: Not Found";
            }

            try
            {
                sensorData5 = ReadI2CTemp(Sensor5);                               // Read Sensor 5
                Sensor5Text = String.Format("Sensor 5: {0:F3} C", sensorData5);   // Save temperature into Text

            }
            catch (Exception ex)
            {
                Sensor5Text = "Sensor 5: Not Found";
            }

            try
            {
                sensorData6 = ReadI2CTemp(Sensor6);                               // Read Sensor 6
                Sensor6Text = String.Format("Sensor 6: {0:F3} C", sensorData6);   // Save temperature into Text
            }
            catch (Exception ex)
            {
                Sensor6Text = "Sensor 6: Not Found";
            }

            var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>    // Upload Text to UI
            {
                Text_Sensor1.Text = Sensor1Text;
                Text_Sensor2.Text = Sensor2Text;
                Text_Sensor3.Text = Sensor3Text;
                Text_Sensor4.Text = Sensor4Text;
                Text_Sensor5.Text = Sensor5Text;
                Text_Sensor6.Text = Sensor6Text;
            });

            string webApiUrl = "http://thermalchamber.azurewebsites.net/sensors/update/" + sensorData1 + "/" + sensorData2 + "/" + sensorData3 + "/" + sensorData4 + "/" + sensorData5 + "/" + sensorData6 + "/";
            CallWebApiUnprotectedAsync(webApiUrl);

        }

        private double ReadI2CTemp(I2cDevice Sensor)       // Read and convert data to Celcius
        {
            byte[] RegAddrBuf = new byte[] { RegReadAddress };  // Read Register from RegReadAddress
            byte[] ReadBuf = new byte[2];                       // Read 2 bytes of data

            Sensor.WriteRead(RegAddrBuf, ReadBuf);
            byte upperByte = ReadBuf[0];
            byte lowerByte = ReadBuf[1];
            upperByte = Convert.ToByte(upperByte & 0x1F);
            var processedUpperByte = (float)upperByte;
            var processedLowerByte = (float)lowerByte;
            double temp;
            if (Convert.ToByte(upperByte & 0x10) == 0x10)
            {
                processedUpperByte = Convert.ToByte(upperByte & 0x0F);
                temp = 256 - (processedUpperByte * 16f + processedLowerByte / 16f);
                Debug.WriteLine(256 - (processedUpperByte * 16f + processedLowerByte / 16f));
            }
            else
            {
                temp = processedUpperByte * 16f + processedLowerByte / 16f;
                Debug.WriteLine(processedUpperByte * 16f + processedLowerByte / 16f);
            }
            return temp;
        }

        private async static void CallWebApiUnprotectedAsync(string webApiUrl)
        {
            try
            {
                HttpClient client = new HttpClient();
                Uri requestURI = new Uri(webApiUrl);
                Debug.WriteLine($"Reading values from '{requestURI}'.");
                HttpResponseMessage httpResponse = await client.GetAsync(requestURI);
                Debug.WriteLine($"HTTP Status Code: '{httpResponse.StatusCode.ToString()}'");
                Debug.WriteLine($"HTTP Response: '{httpResponse.ToString()}'");
                string responseString = await httpResponse.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject(responseString);
                Debug.WriteLine($"JSON Response: {json}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in CallWebApiUnprotectedAsync(): " + ex.Message);
            }
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            Sensor1.Dispose();  // Clear data
            Sensor2.Dispose();
            Sensor3.Dispose();
            Sensor4.Dispose();
            Sensor5.Dispose();
            Sensor6.Dispose();
        }

    }
}
