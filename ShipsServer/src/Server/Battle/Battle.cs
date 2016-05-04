using System;
using System.Collections.Generic;
using System.Linq;
using ShipsServer.Common;
using ShipsServer.Enums;

namespace ShipsServer.Server.Battle
{
    public class Battle
    {
        public int Id { get; set; }
        public BattleStatus Status { get; set; }

        private List<Player> _players;

        public Battle()
        {
            Status = BattleStatus.BATTLE_STATUS_INITIAL;
            _players = new List<Player>(Constants.MAX_BATTLE_PLAYERS);
        }

        public void Finish(Player winner, Player looser)
        {
            Status = BattleStatus.BATTLE_STATUS_DONE;
            if (winner == null || looser == null)
                return;

            winner.Session.BattleStatistics.Wins += 1;
            winner.Session.BattleStatistics.LastBattle = Time.UnixTimeNow();
            looser.Session.BattleStatistics.Loose += 1;
            looser.Session.BattleStatistics.LastBattle = Time.UnixTimeNow();
        }

        public Player AddPlayer(Session session, Board board)
        {
            if (_players.Count > 2)
                throw new IndexOutOfRangeException("AddPlayer overflow. Maximum 2 players.");

            _players.Add(new Player(session, board));
            return _players.Last();
        }

        public Player GetPlayer(int idx)
        {
            if (_players.Count > 2)
                throw new IndexOutOfRangeException("GetPlayer overflow. Maximum 2 players.");

            return _players[idx];
        }

        public Player GetPlayerBySession(Session session)
        {
            return _players.Find(x => x.Session.AccountId == session.AccountId);
        }

        public Player GetOponentPlayer(Session session)
        {
            return _players.Find(x => x.Session.AccountId != session.AccountId);
        }
    }
}
