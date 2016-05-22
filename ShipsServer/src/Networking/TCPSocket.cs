using System;
using System.Net.Sockets;
using ShipsServer.Common;
using ShipsServer.Database;
using ShipsServer.Enums;
using ShipsServer.Protocol;
using ShipsServer.Server;

namespace ShipsServer.Networking
{
    public class TCPSocket
    {
        public Socket Socket { get; private set; }
        public byte[] Buffer { get; private set; }
        public bool IsClosed { get; set; }
        private Session _session;

        public TCPSocket(Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException("socket");

            this.Socket = socket;
            this.Buffer = new byte[256];
            this.IsClosed = false;
            this._session = null;
        }

        ~TCPSocket()
        {
            Socket.Close();
        }

        public void Receive(int bytes)
        {
            if (bytes == 0)
                return;

            byte[] temp = Buffer;
            Array.Resize(ref temp, bytes);
            byte[] decryptBytes = Cryptography.Decrypt(temp);
            var packet = ParsePacket(decryptBytes);
            Array.Clear(Buffer, 0, Buffer.Length);
            if (packet == null)
                return;

            Console.WriteLine($"Receive packet {packet.Opcode} from client {Socket.RemoteEndPoint}");
            PacketReader(packet); // Отправка пакета на выбор хэндлера
        }

        public void SendPacket(Packet packet)
        {
            WriteHeader(packet);
            var encryptBytes = Cryptography.Encrypt(packet.ToArray());
            Console.WriteLine($"Send packet {packet.Opcode} to client {Socket.RemoteEndPoint}");
            AsyncTcpServer.Instanse.Send(this, encryptBytes);
        }

        public void Close()
        {
            Socket.Close();
        }

        private Packet ParsePacket(byte[] bytes)
        {
            var buffer = new ByteBuffer(bytes);
            Opcode opcode = (Opcode)buffer.ReadUInt16();
            UInt16 size = buffer.ReadUInt16();

            if (opcode >= Opcode.MAX_OPCODE)
                return null;

            if (size > 1000)
                return null;

            var packet = new Packet(opcode);
            packet.WriteBytes(buffer.GetBytes(size));
            packet.ResetPos();
            return packet;
        }

        private void WriteHeader(Packet packet)
        {
            byte[] bytes = packet.ToArray();

            packet.Clear();
            packet.WriteUInt16((ushort)packet.Opcode);
            packet.WriteUInt16((ushort)bytes.Length);
            packet.WriteBytes(bytes);
        }

        private bool PacketReader(Packet packet)
        {
            if (packet.Opcode >= Opcode.MAX_OPCODE)
            {
                Console.WriteLine($"PacketReader: Unknown opcode {packet.Opcode} from client {Socket.RemoteEndPoint.ToString()}");
                return true;
            }

            switch (packet.Opcode)
            {
                case Opcode.CMSG_AUTH:
                {
                    HandleAuth(packet);
                    break;
                }
                case Opcode.CMSG_REGISTRATION:
                {
                    HandleRegistration(packet);
                    break;
                }
                case Opcode.CMSG_LOGOUT:
                {
                    _session.IsLogout = true;
                    _session = null;
                    break;
                }
                case Opcode.CMSG_DISCONNECTED:
                {
                    IsClosed = true;
                    break;
                }
                default:
                {
                    if (_session == null)
                    {
                        Console.WriteLine($"PacketReader: Session is null for packet {packet.Opcode} from client {Socket.RemoteEndPoint}");
                        return false;
                    }

                    if (_session.IsLogout)
                        return false;

                    _session.QueuePacket(packet);
                    break;
                }
            }

            return true;
        }

        private void HandleAuth(Packet packet)
        {
            var username = packet.ReadUTF8String();
            var password = packet.ReadUTF8String();

            AuthResponse responseCode = AuthResponse.AUTH_RESPONSE_SUCCESS;

            var mysql = MySQL.Instance();
            var uName = string.Empty;
            uint accountId = 0;
            using (var reader = mysql.Execute($"SELECT `Id`, `username`, `name` FROM `users` WHERE `username` = '{username}' AND `password` = '{MD5Hash.Get(username + ":" + password)}'"))
            {
                if (reader == null || _session != null)
                    responseCode = AuthResponse.AUTH_RESPONSE_UNKNOWN_ERROR;
                else if (!reader.Read())
                    responseCode = AuthResponse.AUTH_RESPONSE_UNKNOWN_USER;

                if (responseCode == AuthResponse.AUTH_RESPONSE_SUCCESS)
                {
                    accountId = reader.GetUInt32(0);
                    uName = reader.GetString(1);
                }
            }

            if (!string.IsNullOrEmpty(uName) && accountId != 0)
            {
                _session = new Session(uName, accountId, this);
                Server.Server.Instance.AddSessionQueue(_session);
            }

            var response = new Packet(Opcode.SMSG_AUTH_RESPONSE);
            response.WriteUInt8((byte)responseCode);
            SendPacket(response);
        }

        private void HandleRegistration(Packet packet)
        {
            string username = packet.ReadUTF8String();
            string password = packet.ReadUTF8String();

            RegistrationResponse responseCode = RegistrationResponse.REG_RESPONSE_SUCCESS;;

            var mysql = MySQL.Instance();
            using (var reader = mysql.Execute($"SELECT `Id` FROM `users` WHERE `username` = '{username}'"))
            {
                if (reader == null)
                    responseCode = RegistrationResponse.REG_RESPONSE_UNKNOWN_ERROR;
                else if (reader.Read())
                    responseCode = RegistrationResponse.REG_RESPONSE_HERE_USER;
            }

            if (responseCode == RegistrationResponse.REG_RESPONSE_SUCCESS)
            {
                var insertId = mysql.PExecute($"INSERT INTO `users` (`username`, `password`) VALUES ('{username}', '{MD5Hash.Get(username + ":" + password)}')");
                if (insertId != -1)
                {
                    _session = new Session(username, (uint)insertId, this);
                    Server.Server.Instance.AddSessionQueue(_session);
                }
                else
                    responseCode = RegistrationResponse.REG_RESPONSE_UNKNOWN_ERROR;
            }

            var response = new Packet(Opcode.SMSG_REGISTRATION_RESPONSE);
            response.WriteUInt8((byte)responseCode);
            SendPacket(response);
        }
    }
}
