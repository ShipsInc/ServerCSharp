using System;
using System.Collections.Generic;
using ShipsServer.Common;
using ShipsServer.Server.Battle.Enums;

namespace ShipsServer.Server.Battle
{
    public class Battle
    {
        public int Id { get; set; }
        public BattleStatus Status { get; set; }

        private Board[] _boards;
        private List<Session> _players;

        public Battle()
        {
            Id = 0;
            Status = BattleStatus.BATTLE_STATUS_INITIAL;
            _boards = new Board[Constants.MAX_BATTLE_PLAYERS];
            _players = new List<Session>(Constants.MAX_BATTLE_PLAYERS);
        }

        public Board GetBoardByAccountId(Session session)
        {
            int plrIdx = _players.FindIndex(s => s.AccountId == session.AccountId);
            if (plrIdx == -1)
                return null;

            return _boards[plrIdx];
        }

        public int AddPlayer(Session session)
        {
            if (_players.Count > 2)
                throw new IndexOutOfRangeException("AddPlayer overflow. Maximum 2 players.");

            _players.Add(session);
            return _players.Count - 1;
        }

        public void SetBoard(int plrIdx, Board board)
        {
            if (plrIdx > Constants.MAX_BATTLE_PLAYERS)
                throw new ArgumentOutOfRangeException("SetBoard player index overflow");

            _boards[plrIdx] = board;
        }
    }
}
