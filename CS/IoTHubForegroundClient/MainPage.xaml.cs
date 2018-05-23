using Newtonsoft.Json.Linq;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using IoTHubForegroundClient.Models;

namespace IoTHubForegroundClient
{
    public sealed partial class MainPage : Page
    {
        static string ConnectionStringFileName = "connection.string.iothub";
        static DeviceClient Client = null;
        static TwinCollection reportedProperties = new TwinCollection();
        static CancellationTokenSource cts;
        static double baseTemperature = 20;

        const string SetTemperature = "setTemperature";
        const string SettingValue = "value";

        private bool disconnect = false;
        private ulong counter = 0;

        bool _running = false;

        public MainPage()
        {
            this.InitializeComponent();
            ConnectStringBox.Text =
                GetConnectionString();
            ConnectButton.Content = "Connect";
            //Start("");
        }

        private string GetConnectionString()
        {
            return "HostName=comp6324IoTHub.azure-devices.net;DeviceId=comp6234-crh-rpi-00001;SharedAccessKey=w9mEMzOLj5ZhTDkjsaLcl6dr5mt0tFnQdmSduzKnD/c=";
        }

        private void Start(string connectionString)
        {
            Debug.WriteLine("Raspberry Pi IoT Central example");

            try
            {
                if (String.IsNullOrEmpty(connectionString))
                {
                    connectionString = GetConnectionString();
                }
                if (String.IsNullOrEmpty(connectionString))
                {
                    return;
                }

                ConnectStringBox.Text = connectionString;

                InitClient(connectionString);

                cts = new CancellationTokenSource();
                SendTelemetryAsync(cts.Token);

                //Debug.WriteLine("Wait for settings update...");
               // await Client.SetDesiredPropertyUpdateCallbackAsync(HandleSettingChanged, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private static void InitClient(string connectionString)
        {
            try
            {
                Debug.WriteLine("Connecting to hub");
                Client = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }


        private async void ShowTelemetry(double temperature, Weather weather)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TemperatureBlock.Text = string.Format("{0:N2}\u00B0C", temperature);
                WeatherBlock.Text = weather.ToString().Replace('_', ' ');
            });
        }

        private async void SendTelemetryAsync(CancellationToken token)
        {
            // If we are already sending telemetry, let's not spawn another one...
            if (_running)
            {
                return;
            }
            _running = true;

            try
            {
                Random rand = new Random();

                while (true)
                {
                    if (disconnect)
                    {
                        disconnect = false;
                        break;
                    }
                    double currentTemperature = baseTemperature + rand.NextDouble() * 10;
                    Weather currentWeather = (Weather)rand.Next(6);
                    ShowTelemetry(currentTemperature, currentWeather);

                    var telemetryDataPoint = new
                    {
                        temp = currentTemperature,
                        weather = (int)currentWeather
                    };
                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));

                    token.ThrowIfCancellationRequested();
                    await Client.SendEventAsync(message);
                    counter++;
                    DataPointsBlock.Text = counter.ToString();
                    Debug.WriteLine("{0} > Sending telemetry: {1}", DateTime.Now, messageString);

                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Intentional shutdown: {0}", ex.Message);
            }
        }

        private static void AcknowledgeSettingChange(TwinCollection desiredProperties, string setting)
        {
            reportedProperties[setting] = new
            {
                value = desiredProperties[setting]["value"],
                status = "completed",
                desiredVersion = desiredProperties["$version"],
                message = "Processed"
            };
        }


        private void OnConnect(object sender, RoutedEventArgs e)
        {
            if (!_running)
            {
                ConnectButton.Content = "Disconnect";
                Start("");
            }
            else
            {
                ConnectButton.Content = "Connect";
                disconnect = true;
            }
        }
    }
}