using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Normalization
{
    public interface IBoardNormalizer
    {
        BoardDelta BuildFallStep(BoardData boardData);
        BoardDelta BuildDestroyStep(BoardData boardData);
    }
}