using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ShipsServer.Networking;
using ShipsServer.Server;
using Timer = ShipsServer.Common.Timer;

namespace ShipsServer
{
    class Program
    {
        private static readonly UInt32 SERVER_SLEEP_CONST = 50;

        private static void ServerUpdateLoop()
        {
            UInt32 realCurrTime = 0;
            UInt32 realPrevTime = Timer.GetMSTime();

            UInt32 prevSleepTime = 0;

            ///- Work server
            while (true)
            {
                ++Server.Server.ServerLoopCounter;
                realCurrTime = Timer.GetMSTime();

                UInt32 diff = Timer.GetMSTimeDiff(realPrevTime, realCurrTime);

                Server.Server.Instance.Update(diff);
                realPrevTime = realCurrTime;

                if (diff <= SERVER_SLEEP_CONST + prevSleepTime)
                {
                    prevSleepTime = SERVER_SLEEP_CONST + prevSleepTime - diff;
                    Thread.Sleep((int)prevSleepTime);
                }
                else
                    prevSleepTime = 0;
            }
        }

        private static void InitialThreads()
        {
            int MaxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);
        }

        static void Main(string[] args)
        {
            InitialThreads();
            Task.Factory.StartNew(() => { new AsyncTcpServer(IPAddress.Parse("127.0.0.1"), 8085).Start(); });
            ServerUpdateLoop();
        }
    }
}
