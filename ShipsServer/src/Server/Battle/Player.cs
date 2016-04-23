using System.Collections.Generic;
using System.Drawing;
using ShipsServer.Enums;

namespace ShipsServer.Server.Battle
{
    public class Player
    {
        public Session Session { get; private set; }
        private readonly Dictionary<Point, ShotResult> PastShots;
        public bool CanShot { get; set; }
        public readonly Board Board;

        public Player(Session session, Board board)
        {
            Session = session;
            CanShot = false;
            PastShots = new Dictionary<Point, ShotResult>();
            Board = board;
        }

        public void AddShotResult(int x, int y, ShotResult result)
        {
            PastShots[new Point(x, y)] = result;
        }

        public void Reset()
        {
            PastShots.Clear();
            CanShot = false;
        }

        public ShotResult Shot(int x, int y, Player oponent)
        {
            if (PastShots.ContainsKey(new Point(x, y)))
                return ShotResult.SHOT_RESULT_YOU_SHOT_IT_CELL;

            var result = oponent.Board.OpenentShotAt(x, y);
            if (result == ShotResult.SHOT_RESULT_MISSED)
                CanShot = false;

            AddShotResult(x, y, result);
            return result;
        }
    }
}
