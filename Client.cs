using System;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Net.NetworkInformation;
using System.Net.Http;

namespace SpeedTestName
{
    class ClientRequest
    {
        private string _localMachine;

        public string GetLocalHostName()
        {
            string hostName = Dns.GetHostName();
            Console.WriteLine($"Local Machine host name: {hostName}");

            _localMachine = hostName;

            return _localMachine;
        }

        public string GetLocalIP(string _localMachine)
        {
            // returns a list of IP addresses
            IPHostEntry ipEntry = Dns.GetHostEntry(_localMachine);

            List<IPAddress> results = new List<IPAddress>();

            foreach(IPAddress address in ipEntry.AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    results.Add(address);
                }
            }

            if (results.Count == 0)
            {
                Console.WriteLine("No IPv4 addresses found");
                return null;
            }

            IPAddress LocalIpAddress = results[0];

            Console.WriteLine($"{_localMachine} IP address: {LocalIpAddress}");

            return LocalIpAddress.ToString();

        }

        public IPAddress[] GetNetworkIPs()
        {
            try
            {
                string hostName = Dns.GetHostName();

                // Get the IPHostEntry for the local machine
                // This contains a list of IP addresses associated with the host
                IPHostEntry ipEntry = Dns.GetHostEntry(hostName);
                IPAddress[] addrList = ipEntry.AddressList;

                Console.WriteLine($"Local IP addresses:");
                
                foreach (IPAddress addr in addrList)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Console.WriteLine($"{addr.AddressFamily}: {addr.ToString()}");
                        Console.WriteLine("***");
                    }
                }

                Console.WriteLine("Successfully wrote all IP addresses!");
                return addrList;
            }

            catch (Exception e)
            {
                Console.WriteLine($"An error occured: {e.Message}");
                return null;
            }
        }

        public void InitServer(string serverIp)
        {
            var ip = IPAddress.Parse(serverIp);
            var endPoint = new IPEndPoint(ip, 11111);

            using Socket listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(endPoint);
            listener.Listen(10);

            Console.WriteLine($"Server listening on {endPoint}");

            while (true)
            {
                using Socket client = listener.Accept();
                Console.WriteLine($"Client connected: {client.RemoteEndPoint}");

                // Wait for "GO" from client (simple sync)
                byte[] smallBuf = new byte[32];
                int n = client.Receive(smallBuf);
                string msg = Encoding.ASCII.GetString(smallBuf, 0, n);

                if (!msg.StartsWith("GO")) continue;

                // Stream data for ~5 seconds (or you can stream a fixed byte count)
                byte[] payload = new byte[64 * 1024]; // 64KB chunks
                new Random().NextBytes(payload);

                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalSeconds < 5)
                {
                    client.Send(payload);
                }

                client.Shutdown(SocketShutdown.Both);
                Console.WriteLine("Finished sending test data.");
            }
        }


        public void InitClient(string serverIp, int port = 11111)
        {
            IPAddress serverAddress = IPAddress.Parse(serverIp);
            IPEndPoint serverEndPoint = new IPEndPoint(serverAddress, port);

            Console.WriteLine("Initiating socket connection...");

            using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Console.WriteLine($"Connecting to server: {serverEndPoint}...");
                sender.Connect(serverEndPoint);

                Console.WriteLine($"Socket connected to - {sender.RemoteEndPoint}");

                byte[] messageSent = Encoding.ASCII.GetBytes("HELLO");
                sender.Send(messageSent);

                sender.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred when handling client...");
                Console.WriteLine(e);
            }
        }

        public async Task<double> GetInternetDownloadSpeedAsync(string url, int secondsToRun = 8)
        {
            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(secondsToRun + 10)
            };

            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync();

            byte[] buffer = new byte[64 * 1024];
            long totalBytes = 0;
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < secondsToRun)
            {
                int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) break;
                totalBytes += read;
            }

            sw.Stop();

            double mbps = (totalBytes * 8.0) / (sw.Elapsed.TotalSeconds * 1_000_000.0);
            Console.WriteLine($"Internet download: {mbps:F2} Mbps");
            return mbps;
        }

        public double GetDownloadSpeed(string serverIP)
        {
            IPAddress serverAddress = IPAddress.Parse(serverIP);
            IPEndPoint remoteEndPoint = new IPEndPoint(serverAddress, 11111);

            Console.WriteLine($"Starting download test from server: {serverIP}");

            using Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            sock.Connect(remoteEndPoint);
            Console.WriteLine($"Connected: {sock.RemoteEndPoint}");

            byte[] go = Encoding.ASCII.GetBytes("GO");
            sock.Send(go);

            byte[] buffer = new byte[64 * 1024];
            long totalBytes = 0;

            var sw = Stopwatch.StartNew();
            while (true)
            {
                int read = sock.Receive(buffer);
                if (read <= 0) break;
                totalBytes += read;
            }
            sw.Stop();

            double mbps = (totalBytes * 8.0) / (sw.Elapsed.TotalSeconds * 1_000_000.0);

            Console.WriteLine($"Received {totalBytes:N0} bytes in {sw.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"Download Speed: {mbps:F2} Mbps");

            return mbps;
        }
    }
}