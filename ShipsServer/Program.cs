using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ShipsServer.Networking;
using Time = ShipsServer.Common.Time;

namespace ShipsServer
{
    class Program
    {
        private static readonly UInt32 SERVER_SLEEP_CONST = 50;

        private static void ServerUpdateLoop()
        {
            int realCurrTime = 0;
            int realPrevTime = Time.GetMSTime();

            int prevSleepTime = 0;

            ///- Work server
            while (true)
            {
                ++Server.Server.ServerLoopCounter;
                realCurrTime = Time.GetMSTime();

                int diff = Time.GetMSTimeDiff(realPrevTime, realCurrTime);

                Server.Server.Instance.Update(diff);
                realPrevTime = realCurrTime;

                if (diff <= SERVER_SLEEP_CONST + prevSleepTime)
                {
                    prevSleepTime = (int)SERVER_SLEEP_CONST + prevSleepTime - diff;
                    Thread.Sleep((int)prevSleepTime);
                }
                else
                    prevSleepTime = 0;
            }
        }

        private static void InitialThreads()
        {
            var maxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(maxThreadsCount, maxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);
        }

        static void Main(string[] args)
        {
            InitialThreads();
            Task.Factory.StartNew(() => { new AsyncTcpServer(IPAddress.Parse("0.0.0.0"), 8085).Start(); });
            ServerUpdateLoop();
            AsyncTcpServer.Instanse.Stop();
        }
    }
}
