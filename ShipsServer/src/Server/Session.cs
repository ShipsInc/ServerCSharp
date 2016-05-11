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
        private Int32 SaveInverval { get; set; }
        private TCPSocket Socket { get; set; }
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

        public Session(string username, UInt32 id, TCPSocket socket)
        {
            Username = username;
            AccountId = id;
            Socket = socket;

            IsLogout = false;
 
            Address = socket.Socket.RemoteEndPoint.ToString();

            _packetQueue = new Queue<Packet>();

            BattleStatistics = new Statistics(id);

            saveSessionTimer = new Timer(Constants.SAVE_INTERVAL) { Enabled = true };
            saveSessionTimer.Elapsed += new ElapsedEventHandler(SaveSession);
        }

        public bool Update()
        {
            if (IsLogout)
                return false;

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

        public void SendPacket(Packet packet)
        {
            if (Socket == null || !Socket.Socket.Connected)
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
