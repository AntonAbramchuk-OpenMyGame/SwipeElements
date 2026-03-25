using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Normalization
{
    public interface IBoardNormalizer
    {
        BoardDeltaSequence Normalize(BoardData boardData);
    }
}