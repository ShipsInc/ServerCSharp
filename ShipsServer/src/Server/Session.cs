using System;
using System.Collections.Generic;
using System.Timers;
using ShipsServer.Common;
using ShipsServer.Networking;
using ShipsServer.Protocol;

namespace ShipsServer.Server
{
    public partial class Session
    {
        private Int32 TimeOutTime { get; set; }
        private Int32 SaveInverval { get; set; }
        private TCPClient Socket { get; set; }
        public string Username { get; private set; }
        public UInt32 AccountId { get; private set; }
        public string Address { get; private set; }
        private Timer saveSessionTimer;
        private bool _logout;
        public bool IsLogout
        {
            get { return _logout; }
            set
            {
                if (!_logout && _logout != value)
                    OnLogout();

                _logout = value;
            }
        }

        public Statistics BattleStatistics { get; set; }
        private Queue<Packet> _packetQueue;

        public Session(string username, UInt32 id, TCPClient client)
        {
            Username = username;
            AccountId = id;
            Socket = client;

            IsLogout = false;

            SaveInverval = (Int32)Constants.SAVE_INTERVAL;

            Address = client.TcpClient.Client.RemoteEndPoint.ToString();

            _packetQueue = new Queue<Packet>();
            ResetTimeOutTime();

            BattleStatistics = new Statistics(id);

            saveSessionTimer = new Timer(15000);
            saveSessionTimer.Enabled = true;
            saveSessionTimer.Elapsed += new ElapsedEventHandler(SaveSession);
        }

        public bool Update(int diff)
        {
            if (IsLogout)
                return false;

            UpdateTimeOutTime(diff);

            if (IsConnectionIdle())
            {
                Socket?.TcpClient.Close();
                return false;
            }

            if (SaveInverval < diff)
            {
                BattleStatistics.SaveToDB();
                SaveInverval = Constants.SAVE_INTERVAL;
            }
            else
                SaveInverval -= (int) diff;

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

        private void UpdateTimeOutTime(int diff)
        {
            TimeOutTime -= (int)diff;
        }

        private void ResetTimeOutTime()
        {
            TimeOutTime = (int)Constants.SOCKET_TIMEOUT;
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

        private void OnLogout()
        {
            BattleStatistics.SaveToDB();
        }

        private void SaveSession(object source, ElapsedEventArgs e)
        {
            BattleStatistics.SaveToDB();
        }
    }
}
