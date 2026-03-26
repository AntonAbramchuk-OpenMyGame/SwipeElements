using System;

namespace OpenMyGame.Core.Board.Data
{
    public struct CellData : IEquatable<CellData>
    {
        public const int EmptyBlockTypeId = -1;
        public const int EmptyBlockId = -1;

        public int BlockTypeId;
        public int BlockId;

        public bool IsEmpty => BlockTypeId == EmptyBlockTypeId;
        public bool IsFilled => BlockTypeId != EmptyBlockTypeId;

        public CellData(int blockTypeId, int blockId)
        {
            BlockTypeId = blockTypeId;
            BlockId = blockId;
        }

        public bool Equals(CellData other)
        {
            return BlockTypeId == other.BlockTypeId && BlockId == other.BlockId;
        }

        public override bool Equals(object obj)
        {
            return obj is CellData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (BlockTypeId * 397) ^ BlockId;
            }
        }

        public static bool operator ==(CellData left, CellData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CellData left, CellData right)
        {
            return !left.Equals(right);
        }

        public static CellData Empty => new(EmptyBlockTypeId, EmptyBlockId);

        public static CellData CreateFilled(int blockTypeId, int blockId)
        {
            if (blockTypeId < 0)
                throw new ArgumentOutOfRangeException(nameof(blockTypeId), "Filled cell block type id must be >= 0.");

            if (blockId < 0)
                throw new ArgumentOutOfRangeException(nameof(blockId), "Filled cell block id must be >= 0.");

            return new CellData(blockTypeId, blockId);
        }
    }
}