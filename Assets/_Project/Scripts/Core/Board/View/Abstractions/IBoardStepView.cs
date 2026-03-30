using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.View.Abstractions
{
    public interface IBoardStepView
    {
        void ApplyMoveStep(BoardDelta delta, System.Action<BoardDelta> onComplete);
        void ApplyFallStep(BoardDelta delta, System.Action<BoardDelta> onComplete);
        void ApplyDestroyStep(BoardDelta delta, System.Action<BoardDelta> onComplete);
    }
}