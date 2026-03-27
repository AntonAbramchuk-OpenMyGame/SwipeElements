using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface IBoardNormalizer
    {
        BoardDelta BuildFallStep(BoardData boardData);
        BoardDelta BuildDestroyStep(BoardData boardData);
    }
}