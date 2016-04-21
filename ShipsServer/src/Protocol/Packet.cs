using System.Runtime.CompilerServices;
using ShipsServer.Networking;

namespace ShipsServer.Protocol
{
    public class Packet : ByteBuffer
    {
        public int Opcode { get; private set; }

        public Packet() : base()
        {
            this.Opcode = 0;
        }

        public Packet(int opcode) : base()
        {
            this.Opcode = opcode;
        }
    }
}
