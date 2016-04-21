using ShipsServer.Server.Battle.Enums;

namespace ShipsServer.Server.Battle
{
    public class BoardCell
    {
        private BoardCellState _state;

        public BoardCell(int x, int y, BoardCellState state = BoardCellState.Normal)
        {
            X = x;
            Y = y;
            this.State = state;
        }

        public BoardCellState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                OnCellStateChenged();
            }
        }

        private void OnCellStateChenged()
        {
            switch (_state)
            {
                case BoardCellState.Normal:
                    break;
                case BoardCellState.MissedShot:
                    break;
                case BoardCellState.Ship:
                     break;
                case BoardCellState.ShotShip:
                    break;
                case BoardCellState.ShipDrag:
                    break;
                case BoardCellState.ShipDragInvalid:
                    break;
                case BoardCellState.ShowDrowned:
                    break;
            }
        }

        public int X { get; private set; }
        public int Y { get; private set; }
    }
}
