using ShipsServer.Enums;

namespace ShipsServer.Server.Battle
{
    public class BoardCell
    {
        public BoardCell(int x, int y, BoardCellState state = BoardCellState.BOARD_CELL_STATE_NORMAL)
        {
            X = x;
            Y = y;
            this.State = state;
        }

        public BoardCellState State { get; set; }
        public int X { get; private set; }
        public int Y { get; private set; }
    }
}
