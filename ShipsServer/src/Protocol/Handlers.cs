using System;
using ShipsServer.Protocol;
using ShipsServer.Server.Battle;
using ShipsServer.Server.Battle.Enums;

namespace ShipsServer.Server
{
    public partial class Session
    {
        public void SelectHandler(Packet packet)
        {
            switch ((Opcodes)packet.Opcode)
            {
                case Opcodes.CMSG_KEEP_ALIVE:
                    HandleKeepAlive(packet);
                    break;
                case Opcodes.CMSG_PROFILE:
                    HandleProfile(packet);
                    break;
                case Opcodes.CMSG_GET_GAMES:
                    HandleGetGames(packet);
                    break;
                case Opcodes.CMSG_BATTLE_INITIALIZATION:
                    HandleBattleInitialization(packet);
                    break;
                default:
                    Console.WriteLine($"Unknown opcode {((Opcodes)packet.Opcode).ToString()}");
                    break;
            }
        }

        private void HandleKeepAlive(Packet packet)
        {
            var response = new Packet((int)Opcodes.SMSG_KEEP_ALIVE);
            Socket.SendPacket(response);
        }

        private void HandleProfile(Packet packet)
        {
            var response = new Packet((int)Opcodes.SMSG_PROFILE_RESPONSE);
            response.WriteUTF8String(Username);
            Socket.SendPacket(response);
        }

        private void HandleGetGames(Packet packet)
        {
            var response = new Packet((int)Opcodes.SMSG_GET_GAMES_RESPONSE);
            response.WriteInt32(BattleMgr.Instance.BattleList.Count);
            foreach (var battle in BattleMgr.Instance.BattleList)
                response.WriteInt32(battle.Id);

            Socket.SendPacket(response);
        }

        private void HandleBattleInitialization(Packet packet)
        {
            var battle = new Battle.Battle();
            int idxPlr = battle.AddPlayer(this);

            Board board = new Board();
            board.ReadPacket(packet);

            battle.SetBoard(idxPlr, board);
            battle.Status = BattleStatus.BATTLE_STATUS_WAIT_OPPONENT;
            BattleMgr.Instance.AddBattle(battle);

            var response = new Packet((int)Opcodes.SMSG_BATTLE_INITIAL_BATTLE);
            response.WriteUInt8((byte)BattleStatus.BATTLE_STATUS_WAIT_OPPONENT);
            board.WritePacket(response);
            Socket.SendPacket(response);
        }
    }
}
