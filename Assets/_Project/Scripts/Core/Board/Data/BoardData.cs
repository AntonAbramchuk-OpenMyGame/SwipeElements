using System;

namespace OpenMyGame.Core.Board.Data
{
    public sealed class BoardData
    {
        private readonly CellData[] _cells;

        public BoardSize Size { get; }

        public int Width => Size.Width;
        public int Height => Size.Height;

        public BoardData(BoardSize size, CellData[] cells)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));

            if (cells.Length != size.CellCount)
                throw new ArgumentException("Cells array length does not match board size.", nameof(cells));

            Size = size;
            _cells = new CellData[cells.Length];
            Array.Copy(cells, _cells, cells.Length);
        }

        public CellData GetCell(BoardCoordinates coordinates)
        {
            ValidateCoordinates(coordinates);
            return _cells[ToIndex(coordinates)];
        }

        public int IndexByID(int id)
        {
            for (var i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];

                if (cell.BlockId == id)
                    return i;
            }

            return -1;
        }

        public void SetCell(BoardCoordinates coordinates, CellData cell)
        {
            ValidateCoordinates(coordinates);
            _cells[ToIndex(coordinates)] = cell;
        }

        public bool IsInside(BoardCoordinates coordinates)
        {
            return Size.IsInside(coordinates);
        }

        public int ToIndex(BoardCoordinates coordinates)
        {
            return coordinates.Y * Width + coordinates.X;
        }

        public BoardCoordinates ToCoordinates(int index)
        {
            if (index < 0 || index >= _cells.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var x = index % Width;
            var y = index / Width;
            return new BoardCoordinates(x, y);
        }

        private void ValidateCoordinates(BoardCoordinates coordinates)
        {
            if (!IsInside(coordinates))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinates),
                    $"Coordinates {coordinates} are outside board {Size}.");
            }
        }
    }
}