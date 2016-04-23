using System;
using ShipsServer.Common;
using ShipsServer.Enums;
using ShipsServer.Protocol;
using ShipsServer.Server.Battle;

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
                case Opcodes.CMSG_BATTLE_SHOT:
                    HandleBattleShot(packet);
                    break;
                case Opcodes.CMSG_BATTLE_JOIN:
                    HandleBattleJoin(packet);
                    break;
                case Opcodes.CMSG_BATTLE_LEAVE:
                    HandleBattleLeave(packet);
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
            var player = battle.AddPlayer(this, new Board());
            player.Board.ReadPacket(packet);

            battle.Status = BattleStatus.BATTLE_STATUS_WAIT_OPPONENT;
            BattleMgr.Instance.AddBattle(battle);

            // Ответ данных
            var response = new Packet((int)Opcodes.SMSG_BATTLE_INITIAL_BATTLE);
            response.WriteInt32(battle.Id);
            response.WriteUInt8((byte)BattleStatus.BATTLE_STATUS_WAIT_OPPONENT);
            Socket.SendPacket(response);
        }

        private void HandleBattleShot(Packet packet)
        {
            var battle = BattleMgr.Instance.GetBattle(packet.ReadInt32());
            if (battle == null)
            {
                var response = new Packet((int)Opcodes.SMSG_BATTLE_RESPONSE);
                response.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_UNKNOWN_ERROR);
                return;
            }

            var player = battle.GetPlayerBySession(this);
            var oponent = battle.GetOponentPlayer(this);
            if (player == null || oponent == null)
            {
                var response = new Packet((int)Opcodes.SMSG_BATTLE_RESPONSE);
                response.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_UNKNOWN_ERROR);
                return;
            }

            if (!player.CanShot)
            {
                var response = new Packet((int)Opcodes.SMSG_BATTLE_RESPONSE);
                response.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_CANT_SHOT);
                return;
            }

            var x = packet.ReadInt16();
            var y = packet.ReadInt16();

            var result = player.Shot(x, y, oponent);

            var opcode = result == ShotResult.SHOT_RESULT_SHIP_DROWNED
                ? Opcodes.SMSG_BATTLE_SHIP_DROWNED
                : Opcodes.SMSG_BATTLE_SHOT_RESULT;

            var shot = new Packet((int)opcode);
            if (shot.Opcode == (int)Opcodes.SMSG_BATTLE_SHOT_RESULT)
            {
                shot.WriteUInt8((byte)result);
                shot.WriteInt16((short)x);
                shot.WriteInt16((short)y);
            }
            else
            {
                Ship ship = oponent.Board.GetShipAt(x, y);
                shot.WriteUInt8((byte)ship.GetShipRegion().GetPoints().Count);
                foreach (var point in ship.GetShipRegion().GetPoints())
                {
                    shot.WriteInt16((short)point.X);
                    shot.WriteInt16((short)point.Y);
                }

                shot.WriteUInt8((byte)ship.Length);
                shot.WriteUInt8((byte)ship.Orientation);
                shot.WriteInt16((short)ship.X);
                shot.WriteInt16((short)ship.Y);
            }

            player.Session.Socket.SendPacket(shot);

            if (result != ShotResult.SHOT_RESULT_MISSED)
            {
                if (oponent.Board.IsAllShipsDrowned())
                {
                    // Игроку который выйграл
                    var finishPacket = new Packet((int)Opcodes.SMSG_BATTLE_FINISH);
                    finishPacket.WriteUInt8(1);
                    player.Session.Socket.SendPacket(finishPacket);

                    // Опоненту
                    finishPacket.Clear();
                    finishPacket.WriteUInt8(0);
                    oponent.Session.Socket.SendPacket(finishPacket);

                    battle.Status = BattleStatus.BATTLE_STATUS_DONE;
                }
                return;
            }

            oponent.CanShot = true;
            oponent.Session.Socket.SendPacket(new Packet((int)Opcodes.SMSG_BATTLE_CAN_SHOT));
        }

        private void HandleBattleJoin(Packet packet)
        {
            var battle = BattleMgr.Instance.GetBattle(packet.ReadInt32());
            if (battle == null)
            {
                var response = new Packet((int)Opcodes.SMSG_BATTLE_RESPONSE);
                response.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_UNKNOWN_ERROR);
                return;
            }

            var player = battle.GetPlayer(0);
            var player2 = battle.AddPlayer(this, new Board());

            player2.Board.ReadPacket(packet);

            battle.Status = BattleStatus.BATTLE_STATUS_BATTLE;

            // Отправка информации о противнике для первого игрока
            var joined = new Packet((int)Opcodes.SMSG_BATTLE_OPONENT_JOINED);
            joined.WriteUTF8String(player.Session.Username);
            joined.WriteUTF8String(player2.Session.Username);
            player.Session.Socket.SendPacket(joined);

            // Отправка информации о противнике для второго игрока
            var plrResponse = new Packet((int)Opcodes.SMSG_BATTLE_RESPONSE);
            plrResponse.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_JOIN_SUCCESS);
            plrResponse.WriteInt32(battle.Id);
            plrResponse.WriteUTF8String(player2.Session.Username);
            plrResponse.WriteUTF8String(player.Session.Username);
            player2.Session.Socket.SendPacket(plrResponse);

            // Выбор случайного игрока для начала
            var canPlayer = battle.GetPlayer(new Random().Next(Constants.MAX_BATTLE_PLAYERS));
            canPlayer.CanShot = true;
            canPlayer.Session.Socket.SendPacket(new Packet((int)Opcodes.SMSG_BATTLE_CAN_SHOT));
        }

        private void HandleBattleLeave(Packet packet)
        {
            var battle = BattleMgr.Instance.GetBattle(packet.ReadInt32());
            if (battle == null)
                return;

            var oponent = battle.GetOponentPlayer(this);
            if (oponent == null)
                return;

            oponent.Session.Socket.SendPacket(new Packet((int)Opcodes.SMSG_BATTLE_OPONENT_LEAVE));
            BattleMgr.Instance.RemoveBattle(battle);
        }
    }
}
