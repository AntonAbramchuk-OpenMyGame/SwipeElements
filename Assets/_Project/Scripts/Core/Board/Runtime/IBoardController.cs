using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Runtime
{
    public interface IBoardController
    {
        void EnqueueMove(BoardMove move);
    }
}