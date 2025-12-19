using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using SpeedTestName; 

namespace Main
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Main");
            ClientRequest usrDevice = new ClientRequest();

            string hostName = usrDevice.GetLocalHostName();

            Console.WriteLine($"Finding {hostName} local machine IP...");
            string usrIP = usrDevice.GetLocalIP(hostName);
            Console.WriteLine($"{usrIP}");
            
            string serverIP = "10.0.0.190";

            // Connectivity Test
            usrDevice.InitClient(serverIP);

            Console.WriteLine("Testing download speed..."); 
            Console.WriteLine($"Client routing to server via {serverIP}");

            double mbps = usrDevice.GetDownloadSpeed(serverIP);

            Console.WriteLine($"Current mbps: {mbps:F2}");
        }
    }
}





