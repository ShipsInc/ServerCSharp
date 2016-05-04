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
        private List<TCPSocket> clients;

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
            this.clients = new List<TCPSocket>();
        }

        public Encoding Encoding { get; set; }

        public void Start()
        {
            Console.WriteLine($"Listener started {Listener.LocalEndpoint.ToString()}");
            this.Listener.Start();
            this.Listener.BeginAcceptTcpClient(AcceptSocketCallback, null);
        }

        public void Stop()
        {
            this.Listener.Stop();
            lock (this.clients)
            {
                foreach (var client in this.clients)
                    client.Socket.Shutdown(SocketShutdown.Both);

                this.clients.Clear();
            }
        }

        public void Send(byte[] bytes)
        {
            lock (this.clients)
            {
                foreach (var client in this.clients)
                {
                    Send(client, bytes);
                }
            }
        }

        public void Send(TCPSocket tcpSocket, byte[] bytes)
        {
            try
            {
                tcpSocket.Socket.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallback), tcpSocket);
            }
            catch (Exception)
            {
                lock (this.clients)
                {
                    this.clients.RemoveAll(x => x.Socket == tcpSocket.Socket);
                }
            }
        }

        private void SendCallback(IAsyncResult result)
        {
            var socket = result.AsyncState as TCPSocket;
            if (socket == null || socket.IsClosed)
                return;

            socket.Socket.EndSend(result);
        }

        private void AcceptSocketCallback(IAsyncResult result)
        {
            var socket = Listener.EndAcceptSocket(result);
            var client = new TCPSocket(socket);
            lock (this.clients)
                this.clients.Add(client);

            socket.BeginReceive(client.Buffer, 0, client.Buffer.Length, 0, new AsyncCallback(ReadCallback), client);
            Listener.BeginAcceptTcpClient(AcceptSocketCallback, null);
        }

        private void ReadCallback(IAsyncResult result)
        {
            var client = result.AsyncState as TCPSocket;
            if (client == null)
                return;

            // Client disconnected
            if (client.IsClosed || !client.Socket.Connected)
                return;

            int bytesRead = 0;
            try
            {
                bytesRead = client.Socket.EndReceive(result);
                client.Recivie(bytesRead);

                Array.Clear(client.Buffer, 0, client.Buffer.Length);
                client.Socket.BeginReceive(client.Buffer, 0, client.Buffer.Length, 0, new AsyncCallback(ReadCallback), client);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return;
            }
        }

        private void UpdateClientsTimer(object source, ElapsedEventArgs e)
        {
            lock (this.clients)
            {
                for (var i = 0; i < this.clients.ToArray().Length; ++i)
                {
                    var client = this.clients.ToArray()[i];
                    if (!client.Socket.Connected || client.IsClosed)
                        this.clients.Remove(client);
                }
            }
        }
    }
}
