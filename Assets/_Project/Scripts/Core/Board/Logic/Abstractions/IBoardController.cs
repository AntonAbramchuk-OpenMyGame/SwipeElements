using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface IBoardController
    {
        void EnqueueMove(BoardMove move);
    }
}