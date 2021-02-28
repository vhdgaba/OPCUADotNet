using System;
using System.Threading;
using System.Windows.Input;
using Opc.UaFx;
using Opc.UaFx.Server;

namespace OPCUATestServer
{
    public class Program
    {
        public static void Main()
        {
            string port = "4080";
            var nodeManager = new NodeManager();

            var server = new OpcServer($"opc.tcp://localhost:{port}/", nodeManager);
            server.Start();
            InfluxConnector.InitializeClient();

            using (var semaphore = new SemaphoreSlim(0))
            {
                var thread = new Thread(() => nodeManager.Simulate(semaphore));
                thread.Start();

                Console.WriteLine($"Server listening on port {port}...");
                Console.ReadKey(true);

                semaphore.Release();
                thread.Join();

                InfluxConnector.DisposeClient();
                server.Stop();
            }
        }
    }
}