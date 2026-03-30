using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface ILevelWinCondition
    {
        bool IsCompleted(BoardData boardData);
    }
}