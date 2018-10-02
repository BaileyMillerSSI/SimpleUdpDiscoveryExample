using ReaderExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Broadcaster
{
    class Program
    {
        private static Task Broadcaster;
        private static Task Listener;

        private static CancellationTokenSource StopWorkerSource = new CancellationTokenSource();
        private static CancellationToken CancelRequestToken
        {
            get
            {
                return StopWorkerSource.Token;
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Searching for available devices");

            StartBroadcaster();
            StartListener();

            Task.WaitAll(Broadcaster, Listener);

        }

        static void StartListener()
        {
            Listener = Task.Factory.StartNew(()=> 
            {
                try
                {
                    TcpListener ServerListener = new TcpListener(IPAddress.Any, DataPort.Listener);
                    ServerListener.Start();
                    while (!CancelRequestToken.IsCancellationRequested)
                    {
                        Socket soc = ServerListener.AcceptSocket();
                        CancelRequestToken.ThrowIfCancellationRequested();

                        Task.Factory.StartNew(()=> 
                        {
                            using (var nStream = new NetworkStream(soc))
                            {
                                using (var reader = new StreamReader(nStream, Encoding.UTF8))
                                {
                                    var data = reader.ReadToEnd();
                                    Console.WriteLine($"Client {((IPEndPoint)soc.RemoteEndPoint).Address} said: \n{data}");
                                }
                            }
                        });

                    }
                }
                catch (Exception ex)
                {
                    
                }
            }, CancelRequestToken);
        }

        static void StartBroadcaster()
        {
            Broadcaster = Task.Factory.StartNew(()=> 
            {
                try
                {
                    while (!CancelRequestToken.IsCancellationRequested)
                    {
                        Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                        ProtocolType.Udp);
                        

                        byte[] sendbuf = Encoding.UTF8.GetBytes("SEND_DIRECTORY_LISTING");
                        IPEndPoint ep = new IPEndPoint(GetBroadcastAddress(), DataPort.Broadcaster);

                        CancelRequestToken.ThrowIfCancellationRequested();
                        s.SendTo(sendbuf, ep);

                        Task.Delay(TimeSpan.FromSeconds(10)).Wait();
                    }
                }
                catch (Exception ex)
                {
                    
                }
            }, CancelRequestToken);
        }

        static IPAddress GetBroadcastAddress()
        {
            var myAddressBroadcast = Dns.GetHostEntryAsync((Dns.GetHostName()))
                                                .Result
                                                .AddressList
                                                .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                                                .Select(x => x)
                                                .ToArray().FirstOrDefault();

            if (myAddressBroadcast != null)
            {
                var oct = myAddressBroadcast.GetAddressBytes();
                oct[3] = 255;

                var IpString = String.Join(".", oct);

                return IPAddress.Parse(IpString);
            }
            else
            {
                return IPAddress.Parse("192.168.1.255");
            }

            
        }
    }
}
