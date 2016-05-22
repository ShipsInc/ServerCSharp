using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ShipsServer.Database;
using ShipsServer.Networking;
using ShipsServer.Protocol.Parser;

namespace ShipsServer
{
    static class Program
    {
        private static readonly UInt32 SERVER_SLEEP_CONST = 5;

        private static void ServerUpdateLoop()
        {
            ///- Work server
            while (true)
            {
                Server.Server.Instance.Update();
                Thread.Sleep((int)SERVER_SLEEP_CONST);
            }
        }

        private static void InitialThreads()
        {
            var maxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(maxThreadsCount, maxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);
        }

        private static void Main(string[] args)
        {
            InitialThreads();
            Console.WriteLine("Loading handlers...");
            Handler.LoadHandlers();

            Console.WriteLine("MySQL Initialization...");
            if (!MySQL.Instance().Initialization())
                return;

            Task.Factory.StartNew(() => { new AsyncTcpServer(IPAddress.Parse("0.0.0.0"), 8085).Start(); });
            var taskServer = Task.Factory.StartNew(ServerUpdateLoop);
            Task.WaitAll(taskServer);
            AsyncTcpServer.Instanse.Stop();
        }
    }
}
