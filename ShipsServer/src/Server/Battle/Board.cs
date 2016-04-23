using System.Collections.Generic;
using System.Linq;
using ShipsServer.Common;
using ShipsServer.Enums;
using ShipsServer.Protocol;

namespace ShipsServer.Server.Battle
{
    public class Board
    {
        private BoardCell[,] _cells;
        private List<Ship> _ships;

        public Board()
        {
            _cells = new BoardCell[Constants.BOARD_SIZE, Constants.BOARD_SIZE];
            _ships = new List<Ship>();
        }

        public void ReadPacket(Packet packet)
        {
            for (var i = 0; i < Constants.BOARD_SIZE; ++i)
            {
                for (var j = 0; j < Constants.BOARD_SIZE; ++j)
                {
                    var state = packet.ReadUInt8();
                    _cells[i, j] = new BoardCell(i, j, (BoardCellState)state);
                }
            }

            var count = packet.ReadInt32();
            for (var i = 0; i < count; ++i)
            {
                var length = packet.ReadUInt8();
                var orientation = packet.ReadUInt8();
                var x = packet.ReadInt16();
                var y = packet.ReadInt16();
                var hitCount = packet.ReadUInt8();
                _ships.Add(new Ship(length, (ShipOrientation)orientation, x, y, hitCount));
            }
        }

        public void WritePacket(Packet packet)
        {
            for (var i = 0; i < Constants.BOARD_SIZE; ++i)
            {
                for (var j = 0; j < Constants.BOARD_SIZE; ++j)
                    packet.WriteUInt8((byte)_cells[i, j].State);
            }

            packet.WriteInt32(_ships.Count);
            foreach (var ship in _ships)
            {
                packet.WriteUInt8((byte)ship.Length);
                packet.WriteUInt8((byte)ship.Orientation);
                packet.WriteInt16((short)ship.X);
                packet.WriteInt16((short)ship.Y);
                packet.WriteUInt8((byte)ship.HitCount);
            }
        }

        public Ship GetShipAt(int x, int y)
        {
            return _ships.FirstOrDefault(ship => ship.IsLocatedAt(x, y));
        }

        public ShotResult OpenentShotAt(int x, int y)
        {
            var ship = GetShipAt(x, y);

            if (ship == null)
            {
                _cells[x, y].State = BoardCellState.BOARD_CELL_STATE_MISSED_SHOT;
                return ShotResult.SHOT_RESULT_MISSED;
            }
            _cells[x, y].State = BoardCellState.BOARD_CELL_STATE_MISSED_SHOT_SHIP;

            ship.HitCount++;

            return ship.IsDrowned ? ShotResult.SHOT_RESULT_SHIP_DROWNED : ShotResult.SHOT_RESULT_SHIP_HIT;
        }

        public bool IsAllShipsDrowned()
        {
            return _ships.All(ship => ship.IsDrowned);
        }
    }
}
