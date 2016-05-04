using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace ShipsServer.Networking
{
    public class AsyncTcpServer
    {
        private TcpListener Listener;
        private List<TCPClient> clients;

        private static object syncRoot = new object();
        private static AsyncTcpServer _instance;

        private Timer _updateClients = new Timer(10000);
        public static AsyncTcpServer Instanse
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_instance == null)
                        {
                            var addr = new IPAddress(new byte[] { 0, 0, 0, 0 });
                            _instance = new AsyncTcpServer(addr, 30000);
                        }
                    }
                }
                return _instance;
            }
        }

        public AsyncTcpServer(IPAddress localaddr, int port) : this()
        {
            this._updateClients.Enabled = true;
            this._updateClients.Elapsed += new ElapsedEventHandler(UpdateClientsTimer);

            this.Listener = new TcpListener(localaddr, port);
        }

        public AsyncTcpServer(IPEndPoint localEP) : this()
        {
            this._updateClients.Enabled = true;
            this._updateClients.Elapsed += new ElapsedEventHandler(UpdateClientsTimer);

            Listener = new TcpListener(localEP);
        }

        private AsyncTcpServer()
        {
            this._updateClients.Enabled = true;
            this._updateClients.Elapsed += new ElapsedEventHandler(UpdateClientsTimer);

            this.Encoding = Encoding.Default;
            this.clients = new List<TCPClient>();
        }

        public Encoding Encoding { get; set; }

        public void Start()
        {
            Console.WriteLine($"Listener started {Listener.LocalEndpoint.ToString()}");
            this.Listener.Start();
            this.Listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
        }

        public void Stop()
        {
            this.Listener.Stop();
            lock (this.clients)
            {
                foreach (var client in this.clients)
                    client.TcpClient.Client.Disconnect(false);

                this.clients.Clear();
            }
        }

        public void Write(byte[] bytes)
        {
            lock (this.clients)
            {
                foreach (var client in this.clients)
                {
                    Write(client.TcpClient, bytes);
                }
            }
        }

        public void Write(TcpClient tcpClient, byte[] bytes)
        {
            try
            {
                var networkStream = tcpClient.GetStream();
                networkStream.BeginWrite(bytes, 0, bytes.Length, WriteCallback, tcpClient);
            }
            catch (Exception)
            {
                lock (this.clients)
                {
                    this.clients.RemoveAll(x => x.TcpClient == tcpClient);
                }
            }
        }

        private void WriteCallback(IAsyncResult result)
        {
            var tcpClient = result.AsyncState as TcpClient;
            if (tcpClient != null && tcpClient.Connected)
            {
                var networkStream = tcpClient.GetStream();
                networkStream.EndWrite(result);
            }
        }

        private void AcceptTcpClientCallback(IAsyncResult result)
        {
            var tcpClient = Listener.EndAcceptTcpClient(result);
            var buffer = new byte[tcpClient.ReceiveBufferSize];
            var client = new TCPClient(tcpClient);
            lock (this.clients)
                this.clients.Add(client);

            var networkStream = client.NetworkStream;
            networkStream.BeginRead(client.Buffer, 0, client.Buffer.Length, ReadCallback, client);
            Listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
        }

        private void ReadCallback(IAsyncResult result)
        {
            var client = result.AsyncState as TCPClient;
            if (client == null)
                return;

            // Client disconnected
            if (client.IsClosed || !client.TcpClient.Connected)
                return;

            NetworkStream networkStream = null;
            int read = 0;
            try
            {
                networkStream = client.NetworkStream;
                read = networkStream.EndRead(result);
            }
            catch (IOException exception)
            {
                Console.WriteLine(exception.Message);
                return;
            }

            client.Recivie(read);
            networkStream.BeginRead(client.Buffer, 0, client.Buffer.Length, ReadCallback, client);
        }

        private void UpdateClientsTimer(object source, ElapsedEventArgs e)
        {
            lock (this.clients)
            {
                for (var i = 0; i < this.clients.ToArray().Length; ++i)
                {
                    var client = this.clients.ToArray()[i];
                    if (!client.TcpClient.Connected || client.IsClosed)
                        this.clients.Remove(client);
                }
            }
        }
    }
}
