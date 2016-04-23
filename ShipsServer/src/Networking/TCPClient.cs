using System;
using System.Net.Sockets;
using ShipsServer.Common;
using ShipsServer.Database;
using ShipsServer.Enums;
using ShipsServer.Protocol;
using ShipsServer.Server;

namespace ShipsServer.Networking
{
    public class TCPClient
    {
        public TcpClient TcpClient { get; private set; }
        public byte[] Buffer { get; private set; }
        public NetworkStream NetworkStream => TcpClient.GetStream();

        private Session _session;

        public TCPClient(TcpClient tcpClient)
        {
            if (tcpClient == null)
                throw new ArgumentNullException("tcpClient");

            this.TcpClient = tcpClient;
            this.Buffer = new byte[256];
            _session = null;
        }

        ~TCPClient()
        {
            TcpClient.Close();
        }

        public void Recivie(int bytes)
        {
            if (bytes == 0)
                return;

            Packet packet = ParsePacket(Buffer);
            Array.Clear(Buffer, 0, Buffer.Length);
            if (packet == null)
                return;

            Console.WriteLine($"Recivie packet {((Opcodes)packet.Opcode).ToString()} from client {TcpClient.Client.RemoteEndPoint.ToString()}");
            PacketReader(packet); // Отправка пакета на выбор хэндлера
        }

        public void SendPacket(Packet packet)
        {
            WriteHeader(packet);
            Console.WriteLine($"Send packet {((Opcodes)packet.Opcode).ToString()} to client {TcpClient.Client.RemoteEndPoint.ToString()}");
            AsyncTcpServer.Instanse.Write(TcpClient, packet.ToArray());
        }

        public void Close()
        {
            TcpClient.Close();
        }

        private Packet ParsePacket(byte[] bytes)
        {
            var buffer = new ByteBuffer(bytes);
            UInt16 opcode = buffer.ReadUInt16();
            UInt16 size = buffer.ReadUInt16();

            if ((Opcodes)opcode >= Opcodes.MAX_OPCODE)
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
            if ((Opcodes) packet.Opcode >= Opcodes.MAX_OPCODE)
            {
                Console.WriteLine($"PacketReader: Unknown opcode {packet.Opcode} from client {TcpClient.Client.RemoteEndPoint.ToString()}");
                return true;
            }

            switch ((Opcodes)packet.Opcode)
            {
                case Opcodes.CMSG_AUTH:
                {
                    HandleAuth(packet);
                    break;
                }
                case Opcodes.CMSG_REGISTRATION:
                {
                    HandleRegistration(packet);
                    break;
                }
                case Opcodes.CMSG_LOGOUT:
                {
                    _session = null;
                    break;
                }
                default:
                {
                    if (_session == null)
                    {
                        Console.WriteLine($"PacketReader: Session is null for packet {((Opcodes)packet.Opcode).ToString()} from client {TcpClient.Client.RemoteEndPoint.ToString()}");
                        return false;
                    }

                    _session.QueuePacket(packet);
                    break;
                }
            }

            return true;
        }

        public void HandleAuth(Packet packet)
        {
            string username = packet.ReadUTF8String();
            string password = packet.ReadUTF8String();

            AuthResponse responseCode = AuthResponse.AUTH_RESPONSE_SUCCESS;

            var mysql = MySQL.Instance();
            using (var reader = mysql.Execute($"SELECT `Id`, `username`, `name` FROM `users` WHERE `username` = '{username}' AND `password` = '{MD5Hash.Get(username + ":" + password)}'"))
            {
                if (reader == null || _session != null)
                    responseCode = AuthResponse.AUTH_RESPONSE_UNKNOWN_ERROR;
                else if (!reader.Read())
                    responseCode = AuthResponse.AUTH_RESPONSE_UNKNOWN_USER;

                if (responseCode == AuthResponse.AUTH_RESPONSE_SUCCESS)
                {
                    _session = new Session(reader.GetString(1), reader.GetUInt32(0), this);
                    Server.Server.Instance.AddSessionQueue(_session);
                }
            }

            var response = new Packet((int)Opcodes.SMSG_AUTH_RESPONSE);
            response.WriteUInt8((byte)responseCode);
            SendPacket(response);
        }

        public void HandleRegistration(Packet packet)
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
                int insertId = mysql.PExecute($"INSERT INTO `users` (`username`, `password`) VALUES ('{username}', '{MD5Hash.Get(username + ":" + password)}')");
                if (insertId != -1)
                {
                    _session = new Server.Session(username, (uint)insertId, this);
                    Server.Server.Instance.AddSessionQueue(_session);
                }
                else
                    responseCode = RegistrationResponse.REG_RESPONSE_UNKNOWN_ERROR;
            }

            var response = new Packet((int)Opcodes.SMSG_REGISTRATION_RESPONSE);
            response.WriteUInt8((byte)responseCode);
            SendPacket(response);
        }
    }
}
