using System;

namespace OpenMyGame.Core.Board.Data
{
    public struct CellData : IEquatable<CellData>
    {
        public const int EmptyBlockTypeId = -1;

        public int BlockTypeId;

        public bool IsEmpty => BlockTypeId == EmptyBlockTypeId;
        public bool IsFilled => BlockTypeId != EmptyBlockTypeId;

        public CellData(int blockTypeId)
        {
            BlockTypeId = blockTypeId;
        }

        public bool Equals(CellData other)
        {
            return BlockTypeId == other.BlockTypeId;
        }

        public override bool Equals(object obj)
        {
            return obj is CellData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return BlockTypeId;
        }

        public static bool operator ==(CellData left, CellData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CellData left, CellData right)
        {
            return !left.Equals(right);
        }

        public static CellData Empty => new(EmptyBlockTypeId);

        public static CellData CreateFilled(int blockTypeId)
        {
            if (blockTypeId < 0)
                throw new ArgumentOutOfRangeException(nameof(blockTypeId), "Filled cell block type id must be >= 0.");

            return new CellData(blockTypeId);
        }
    }
}