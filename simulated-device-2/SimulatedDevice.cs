// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace simulated_device
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;
        private static DeviceClient s_deviceClient2;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity connection-string show --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private readonly static string s_connectionString = "HostName=Team3Hub.azure-devices.net;DeviceId=Team3DotnetDevice;SharedAccessKey=ybbWIm9mWhhI66W8j4YtFCWrWOoJlIvCyKWMy7GdrAM=";
        private readonly static string s_connectionString2 = "HostName=Team3Hub.azure-devices.net;DeviceId=Team3DotnetDevice2;SharedAccessKey=Uc3v/uYNX/qwlLaFdEJtlDZcRX7NkFUTK7//PEox2GA=";

        private static int s_telemetryInterval = 1; // Seconds

        // Handle the direct method call
        private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            // Check the payload is a single integer value
            if (Int32.TryParse(data, out s_telemetryInterval))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Telemetry interval set to {0} seconds", data);
                Console.ResetColor();

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        // Async method to send simulated telemetry
        private static async void SendDeviceToCloudMessagesAsync(DeviceClient client, String bebe)
        {
            // Initial telemetry values
            Random rand = new Random();

            while (true)
            {
                int currentoxygenSaturation =  rand.Next(80,100) ;
                int currentheartRate = rand.Next(90,170) ;

                // Create JSON message
                var telemetryDataPoint = new
                {
                    oxygenSaturation = currentoxygenSaturation,
                    heartRate = currentheartRate
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.Properties.Add("oxygenSaturationAlert", (currentoxygenSaturation < 93) ? "true" : "false");
                message.Properties.Add("heartRateAlert", (currentheartRate < 120 || currentheartRate > 160) ? "true" : "false");

                // Send the telemetry message
                await client.SendEventAsync(message);
                Console.WriteLine("{0}: {1} > Sending message: {2}", bebe,DateTime.Now, messageString);

                await Task.Delay(s_telemetryInterval * 1000);
            }
        }
        private static void Main(string[] args)
        {
            Console.WriteLine("IoT Hub Quickstarts #2 - Simulated device. Ctrl-C to exit.\n");

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);
       
            s_deviceClient2 = DeviceClient.CreateFromConnectionString(s_connectionString2, TransportType.Mqtt);

            // Create a handler for the direct method call
            s_deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null).Wait();
            SendDeviceToCloudMessagesAsync(s_deviceClient,"Bebe 1");
         
            // Create a handler for the direct method call
            s_deviceClient2.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null).Wait();
            SendDeviceToCloudMessagesAsync(s_deviceClient2,"Bebe 2");
            Console.ReadLine();
        }
    }
}
