using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

namespace ShipsServer.Networking
{
    public class AsyncTcpServer
    {
        private TcpListener Listener;
        private List<TCPClient> clients;

        private static object syncRoot = new object();
        private static AsyncTcpServer _instance;

        private System.Timers.Timer UpdateClients = new System.Timers.Timer(10000);
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
                            IPAddress addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
                            _instance = new AsyncTcpServer(addr, 30000);
                        }
                    }
                }
                return _instance;
            }
        }

        public AsyncTcpServer(IPAddress localaddr, int port) : this()
        {
            UpdateClients.Enabled = true;
            UpdateClients.Elapsed += new ElapsedEventHandler(UpdateClientsTimer);

            Listener = new TcpListener(localaddr, port);
        }

        public AsyncTcpServer(IPEndPoint localEP) : this()
        {
            UpdateClients.Enabled = true;
            UpdateClients.Elapsed += new ElapsedEventHandler(UpdateClientsTimer);

            Listener = new TcpListener(localEP);
        }

        private AsyncTcpServer()
        {
            UpdateClients.Enabled = true;
            UpdateClients.Elapsed += new ElapsedEventHandler(UpdateClientsTimer);

            this.Encoding = Encoding.Default;
            this.clients = new List<TCPClient>();
        }

        public Encoding Encoding { get; set; }

        public IEnumerable<TcpClient> Clients
        {
            get
            {
                foreach (TCPClient client in this.clients)
                {
                    yield return client.TcpClient;
                }
            }
        }

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
                foreach (TCPClient client in this.clients)
                {
                    client.TcpClient.Client.Disconnect(false);
                }
                this.clients.Clear();
            }
        }

        public void Write(TcpClient tcpClient, string data)
        {
            byte[] bytes = this.Encoding.GetBytes(data);
            Write(tcpClient, bytes);
        }

        public void Write(string data)
        {
            foreach (TCPClient client in this.clients)
            {
                Write(client.TcpClient, data);
            }
        }

        public void Write(byte[] bytes)
        {
            foreach (TCPClient client in this.clients)
            {
                Write(client.TcpClient, bytes);
            }
        }

        public void Write(TcpClient tcpClient, byte[] bytes)
        {
            try
            {
                NetworkStream networkStream = tcpClient.GetStream();
                networkStream.BeginWrite(bytes, 0, bytes.Length, WriteCallback, tcpClient);
            }
            catch (Exception)
            {
                this.clients.RemoveAll(x => x.TcpClient == tcpClient);
            }
        }

        private void WriteCallback(IAsyncResult result)
        {
            TcpClient tcpClient = result.AsyncState as TcpClient;
            if (tcpClient.Connected)
            {
                NetworkStream networkStream = tcpClient.GetStream();
                networkStream.EndWrite(result);
            }
        }

        private void AcceptTcpClientCallback(IAsyncResult result)
        {
            TcpClient tcpClient = Listener.EndAcceptTcpClient(result);
            byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
            TCPClient client = new TCPClient(tcpClient);
            lock (this.clients)
                this.clients.Add(client);

            NetworkStream networkStream = client.NetworkStream;
            networkStream.BeginRead(client.Buffer, 0, client.Buffer.Length, ReadCallback, client);
            Listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
        }

        private void ReadCallback(IAsyncResult result)
        {
            TCPClient client = result.AsyncState as TCPClient;
            if (client == null)
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
                foreach (TCPClient client in this.clients.ToArray())
                {
                    if (!client.TcpClient.Connected)
                        this.clients.Remove(client);
                }
            }
        }
    }
}
