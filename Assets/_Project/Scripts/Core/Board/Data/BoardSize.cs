using System;

namespace OpenMyGame.Core.Board.Data
{
    [Serializable]
    public readonly struct BoardSize : IEquatable<BoardSize>
    {
        public readonly int Width;
        public readonly int Height;

        public BoardSize(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be > 0.");

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be > 0.");

            Width = width;
            Height = height;
        }

        public int CellCount => Width * Height;

        public bool IsInside(BoardCoordinates coordinates)
        {
            return coordinates.X >= 0 &&
                   coordinates.X < Width &&
                   coordinates.Y >= 0 &&
                   coordinates.Y < Height;
        }

        public bool Equals(BoardSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }

        public static bool operator ==(BoardSize left, BoardSize right) => left.Equals(right);
        public static bool operator !=(BoardSize left, BoardSize right) => !left.Equals(right);
    }
}