using System;

namespace OpenMyGame.Core.Board.Data
{
    [Serializable]
    public readonly struct BoardCoordinates : IEquatable<BoardCoordinates>
    {
        public readonly int X;
        public readonly int Y;

        public BoardCoordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(BoardCoordinates other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardCoordinates other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(BoardCoordinates left, BoardCoordinates right) => left.Equals(right);
        public static bool operator !=(BoardCoordinates left, BoardCoordinates right) => !left.Equals(right);
    }
}