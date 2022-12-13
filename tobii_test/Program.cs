using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tobii.StreamEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Tobii.StreamEngine.Sample
{
    public static class StreamSample
    {
        static Socket client;
        private static void OnGazePoint(ref tobii_gaze_point_t gazePoint, IntPtr userData)
        {
            // Check that the data is valid before using it
            EndPoint point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7000);
            if (gazePoint.validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                string msg = $"{gazePoint.position.x}, {gazePoint.position.y}";
                Console.WriteLine($"Gaze point: {gazePoint.position.x},{gazePoint.position.y}");
                client.SendTo(Encoding.UTF8.GetBytes(msg), point);
            }
        }

        public static void Main()
        {
            //
            client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            Console.WriteLine("Start");

            // Create API context
            IntPtr apiContext;
            tobii_error_t result = Interop.tobii_api_create(out apiContext, null);
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

            // Enumerate devices to find connected eye trackers
            List<string> urls;
            result = Interop.tobii_enumerate_local_device_urls(apiContext, out urls);
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
            if (urls.Count == 0)
            {
                Console.WriteLine("Error: No device found");
                return;
            }

            // Connect to the first tracker found
            IntPtr deviceContext;
            result = Interop.tobii_device_create(apiContext, urls[0], Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out deviceContext);
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

            // Subscribe to gaze data
            result = Interop.tobii_gaze_point_subscribe(deviceContext, OnGazePoint);
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

            

            // This sample will collect 1000 gaze points
            while (true)
            {
                // Optionally block this thread until data is available. Especially useful if running in a separate thread.
                Interop.tobii_wait_for_callbacks(new[] { deviceContext });
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR || result == tobii_error_t.TOBII_ERROR_TIMED_OUT);

                // Process callbacks on this thread if data is available
                Interop.tobii_device_process_callbacks(deviceContext);
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
            }


            // Cleanup
            result = Interop.tobii_gaze_point_unsubscribe(deviceContext);
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
            result = Interop.tobii_device_destroy(deviceContext);
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
            result = Interop.tobii_api_destroy(apiContext);
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
        }

        
    }
}