using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface IBoardService
    {
        BoardDelta ApplyMoveStep(BoardData boardData, BoardMove move);
        BoardDelta BuildFallStep(BoardData boardData);
        BoardDelta BuildDestroyStep(BoardData boardData);
    }
}