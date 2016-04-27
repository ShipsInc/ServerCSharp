using System;
using System.Collections.Generic;
using ShipsServer.Networking;
using ShipsServer.Protocol;

namespace ShipsServer.Server
{
    public partial class Session
    {
        private static UInt32 SOCKET_TIMEOUT = 30000;
        private Int32 TimeOutTime { get; set; }
        private TCPClient Socket { get; set; }
        public string Username { get; private set; }
        public UInt32 AccountId { get; private set; }
        public string Address { get; private set; }

        public bool IsLogout { get; set; }

        private Queue<Packet> _packetQueue;

        public Session(string username, UInt32 id, TCPClient client)
        {
            Username = username;
            AccountId = id;
            Socket = client;

            IsLogout = false;

            Address = client.TcpClient.Client.RemoteEndPoint.ToString();

            _packetQueue = new Queue<Packet>();
            ResetTimeOutTime();
        }

        public bool Update(UInt32 diff)
        {
            if (IsLogout)
            {
                Socket?.TcpClient.Close();
                return false;
            }

            UpdateTimeOutTime(diff);

            if (IsConnectionIdle())
            {
                Socket?.TcpClient.Close();
                return false;
            }

            if (_packetQueue.Count != 0)
            {
                lock (_packetQueue)
                {
                    while (_packetQueue.Count != 0)
                    {
                        var packet = _packetQueue.Dequeue();
                        SelectHandler(packet);
                    }
                }
            }

            return true;
        }

        public void KickPlayer()
        {
            Socket?.Close();
        }

        private void UpdateTimeOutTime(UInt32 diff)
        {
            TimeOutTime -= (Int32)diff;
        }

        private void ResetTimeOutTime()
        {
            TimeOutTime = (Int32)SOCKET_TIMEOUT;
        }

        private bool IsConnectionIdle()
        {
            return TimeOutTime <= 0;
        }

        public void SendPacket(Packet packet)
        {
            if (Socket == null || !Socket.TcpClient.Connected)
                return;

            Socket.SendPacket(packet);
        }

        public void QueuePacket(Packet packet)
        {
            lock (_packetQueue)
            {
                _packetQueue.Enqueue(packet);
            }
        }
    }
}
