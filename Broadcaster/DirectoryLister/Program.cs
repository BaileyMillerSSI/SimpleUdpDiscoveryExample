using ReaderExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryLister
{
    class Program
    {
        static UdpClient listener = new UdpClient(DataPort.Broadcaster);
        static IPEndPoint groupEP;

        static int StartListener()
        {
            Console.WriteLine("Waiting for UDP broadcasts");
            groupEP = new IPEndPoint(IPAddress.Any, DataPort.Broadcaster);
            try
            {
                byte[] bytes = listener.Receive(ref groupEP);

                Console.WriteLine("Received broadcast from {0} :\n {1}\n",
                    groupEP.ToString(),
                    Encoding.UTF8.GetString(bytes, 0, bytes.Length));

                Task.Run(() => {
                    ReturnDirectoryListing(groupEP);
                }).Wait();

                return 0;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
            finally
            {
                listener.Close();
            }
        }

        static int Main(string[] args)
        {
            return StartListener();
        }

        private static void SpawnChildren(int appCount)
        {
            for (int i = 0; i < appCount; i++)
            {
                ProcessStartInfo psi = new ProcessStartInfo(System.Reflection.Assembly.GetEntryAssembly().Location,i.ToString());
                Process p = new Process() {  StartInfo = psi };
                p.Start();
            }
        }

        static void ReturnDirectoryListing(IPEndPoint Endpoint)
        {
            var fileListing = new DirectoryInfo(Environment.CurrentDirectory).GetFiles("*",SearchOption.AllDirectories);

            var builder = new StringBuilder();

            foreach (var file in fileListing.OrderBy(x=>x.Name))
            {
                builder.AppendLine(file.Name);
            }

            var client = new TcpClient();

            try
            {
                client.Connect(Endpoint.Address, DataPort.Listener);
                using (var nStream = client.GetStream())
                {// The stream that handles communcation over the socket
                    using (var writer = new StreamWriter(nStream, Encoding.UTF8))
                    {// The stream that handles communication between the socket and converting it to strings
                        writer.Write(builder.ToString());
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                client.Close();
            }
        }
    }
}
