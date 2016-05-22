using System;
using ShipsServer.Common;
using ShipsServer.Enums;
using ShipsServer.Protocol.Parser;
using ShipsServer.Server;
using ShipsServer.Server.Battle;

namespace ShipsServer.Protocol
{
    public class Handlers
    {
        [Parser(Opcode.CMSG_KEEP_ALIVE)]
        public static void HandleKeepAlive(Session session, Packet packet)
        {
            var response = new Packet(Opcode.SMSG_KEEP_ALIVE);
            session.Socket.SendPacket(response);
        }

        [Parser(Opcode.CMSG_PROFILE)]
        public static void HandleProfile(Session session, Packet packet)
        {
            var response = new Packet(Opcode.SMSG_PROFILE_RESPONSE);
            response.WriteUTF8String(session.Username);
            session.Socket.SendPacket(response);
        }

        [Parser(Opcode.CMSG_GET_GAMES)]
        public static void HandleGetGames(Session session, Packet packet)
        {
            var response = new Packet(Opcode.SMSG_GET_GAMES_RESPONSE);
            response.WriteInt32(BattleMgr.Instance.BattleList.Count);
            foreach (var battle in BattleMgr.Instance.BattleList)
                response.WriteInt32(battle.Id);

            session.Socket.SendPacket(response);
        }

        [Parser(Opcode.CMSG_BATTLE_INITIALIZATION)]
        public static void HandleBattleInitialization(Session session, Packet packet)
        {
            var battle = new Server.Battle.Battle();
            var player = battle.AddPlayer(session, new Board());
            player.Board.ReadPacket(packet);

            battle.Status = BattleStatus.BATTLE_STATUS_WAIT_OPONENT;
            BattleMgr.Instance.AddBattle(battle);

            // Ответ данных
            var response = new Packet(Opcode.SMSG_BATTLE_INITIAL_BATTLE);
            response.WriteInt32(battle.Id);
            response.WriteUInt8((byte)BattleStatus.BATTLE_STATUS_WAIT_OPONENT);
            session.Socket.SendPacket(response);
        }

        [Parser(Opcode.CMSG_BATTLE_SHOT)]
        public static void HandleBattleShot(Session session, Packet packet)
        {
            var battle = BattleMgr.Instance.GetBattle(packet.ReadInt32());
            if (battle == null)
            {
                var response = new Packet(Opcode.SMSG_BATTLE_RESPONSE);
                response.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_UNKNOWN_ERROR);
                return;
            }

            var player = battle.GetPlayerBySession(session);
            var oponent = battle.GetOponentPlayer(session);
            if (player == null || oponent == null)
            {
                var response = new Packet(Opcode.SMSG_BATTLE_RESPONSE);
                response.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_UNKNOWN_ERROR);
                return;
            }

            if (!player.CanShot)
            {
                var response = new Packet(Opcode.SMSG_BATTLE_RESPONSE);
                response.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_CANT_SHOT);
                return;
            }

            var x = packet.ReadUInt8();
            var y = packet.ReadUInt8();

            var result = player.Shot(x, y, oponent);

            var opcode = result == ShotResult.SHOT_RESULT_SHIP_DROWNED
                ? Opcode.SMSG_BATTLE_SHIP_DROWNED
                : Opcode.SMSG_BATTLE_SHOT_RESULT;

            var shot = new Packet(opcode);
            if (shot.Opcode == Opcode.SMSG_BATTLE_SHOT_RESULT)
            {
                shot.WriteUInt8((byte)result);
                shot.WriteUInt8((byte)x);
                shot.WriteUInt8((byte)y);
            }
            else
            {
                var ship = oponent.Board.GetShipAt(x, y);
                shot.WriteUInt8((byte)ship.GetShipRegion().GetPoints().Count);
                foreach (var point in ship.GetShipRegion().GetPoints())
                {
                    shot.WriteUInt8((byte)point.X);
                    shot.WriteUInt8((byte)point.Y);
                }

                shot.WriteUInt8((byte)ship.Length);
                shot.WriteUInt8((byte)ship.Orientation);
                shot.WriteUInt8((byte)ship.X);
                shot.WriteUInt8((byte)ship.Y);
            }

            player.Session.SendPacket(shot);

            var oponentShotResult = new Packet(Opcode.SMSG_BATTLE_OPONENT_SHOT_RESULT);
            oponentShotResult.WriteUInt8((byte)result);
            oponentShotResult.WriteUInt8((byte)x);
            oponentShotResult.WriteUInt8((byte)y);
            oponent.Session.SendPacket(oponentShotResult);

            if (result != ShotResult.SHOT_RESULT_MISSED)
            {
                // Все корабли противника разрушены
                if (oponent.Board.IsAllShipsDrowned())
                {
                    // Игроку который выйграл
                    var finishPacket = new Packet(Opcode.SMSG_BATTLE_FINISH);
                    finishPacket.WriteUInt8(1);
                    player.Session.SendPacket(finishPacket);

                    // Опоненту
                    finishPacket.Clear();
                    finishPacket.WriteUInt8(0);
                    oponent.Session.SendPacket(finishPacket);

                    battle.Finish(player, oponent);
                }
                return;
            }

            oponent.CanShot = true;
            oponent.Session.SendPacket(new Packet(Opcode.SMSG_BATTLE_CAN_SHOT));
        }

        [Parser(Opcode.CMSG_BATTLE_JOIN)]
        public static void HandleBattleJoin(Session session, Packet packet)
        {
            var battle = BattleMgr.Instance.GetBattle(packet.ReadInt32());
            if (battle == null)
            {
                var response = new Packet(Opcode.SMSG_BATTLE_RESPONSE);
                response.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_UNKNOWN_ERROR);
                return;
            }

            var player = battle.GetPlayer(0);
            var player2 = battle.AddPlayer(session, new Board());

            player2.Board.ReadPacket(packet);

            battle.Status = BattleStatus.BATTLE_STATUS_BATTLE;

            // Отправка информации о противнике для первого игрока
            var joined = new Packet(Opcode.SMSG_BATTLE_OPONENT_JOINED);
            joined.WriteUTF8String(player.Session.Username);
            joined.WriteUTF8String(player2.Session.Username);
            player.Session.SendPacket(joined);

            // Отправка информации о противнике для второго игрока
            var plrResponse = new Packet(Opcode.SMSG_BATTLE_RESPONSE);
            plrResponse.WriteUInt8((byte)BattleResponse.BATTLE_RESPONSE_JOIN_SUCCESS);
            plrResponse.WriteInt32(battle.Id);
            plrResponse.WriteUTF8String(player2.Session.Username);
            plrResponse.WriteUTF8String(player.Session.Username);
            player2.Session.SendPacket(plrResponse);

            // Выбор случайного игрока для начала
            var canPlayer = battle.GetPlayer(new Random().Next(Constants.MAX_BATTLE_PLAYERS));
            canPlayer.CanShot = true;
            canPlayer.Session.SendPacket(new Packet(Opcode.SMSG_BATTLE_CAN_SHOT));
        }

        [Parser(Opcode.CMSG_BATTLE_LEAVE)]
        public static void HandleBattleLeave(Session session, Packet packet)
        {
            var battle = BattleMgr.Instance.GetBattle(packet.ReadInt32());
            if (battle == null)
                return;

            // Выход во врея игры
            var oponent = battle.GetOponentPlayer(session);
            battle.Finish(oponent, battle.GetPlayerBySession(session));
            oponent?.Session.SendPacket(new Packet(Opcode.SMSG_BATTLE_OPONENT_LEAVE));
        }

        [Parser(Opcode.CMSG_GET_STATISTICS)]
        public static void HandleGetStatistics(Session session, Packet packet)
        {
            var response = new Packet(Opcode.SMSG_GET_STATISTICS_RESPONSE);
            response.WriteUInt32((uint)session.BattleStatistics.LastBattle);
            response.WriteUInt16((ushort)session.BattleStatistics.Wins);
            response.WriteUInt16((ushort)session.BattleStatistics.Loose);
            session.Socket.SendPacket(response);
        }

        [Parser(Opcode.CMSG_CHAT_SEND_MESSAGE)]
        public static void HandleChatSendMessage(Session session, Packet packet)
        {
            var battle = BattleMgr.Instance.GetBattle(packet.ReadInt32());
            if (battle == null)
                return;

            var text = packet.ReadUTF8String();
            if (string.IsNullOrEmpty(text))
                return;

            var oponent = battle.GetOponentPlayer(session);
            var response = new Packet(Opcode.SMSG_CHAT_MESSAGE);
            response.WriteUTF8String(session.Username);
            response.WriteUTF8String(text);
            oponent?.Session.SendPacket(response);
        }
    }
}
