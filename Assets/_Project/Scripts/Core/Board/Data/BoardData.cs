using System;

namespace OpenMyGame.Core.Board.Data
{
    public sealed class BoardData
    {
        private readonly CellData[] _cells;

        public BoardSize Size { get; }

        public int Width => Size.Width;
        public int Height => Size.Height;

        public CellData this[int x, int y]
        {
            get => _cells[y * Width + x];
            set => _cells[y * Width + x] = value;
        }

        public CellData this[BoardCoordinates coordinates]
        {
            get => GetCell(coordinates);
            set => SetCell(coordinates, value);
        }

        public BoardData(BoardSize size)
        {
            Size = size;
            _cells = new CellData[size.CellCount];

            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = CellData.Empty;
        }

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

        public void SetCell(BoardCoordinates coordinates, CellData cell)
        {
            ValidateCoordinates(coordinates);
            _cells[ToIndex(coordinates)] = cell;
        }

        public bool IsInside(BoardCoordinates coordinates)
        {
            return Size.IsInside(coordinates);
        }

        public BoardData Clone()
        {
            return new BoardData(Size, _cells);
        }

        public int ToIndex(BoardCoordinates coordinates)
        {
            return coordinates.Y * Width + coordinates.X;
        }

        public BoardCoordinates ToCoordinates(int index)
        {
            if (index < 0 || index >= _cells.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            int x = index % Width;
            int y = index / Width;
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