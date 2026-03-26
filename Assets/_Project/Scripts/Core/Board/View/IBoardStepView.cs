using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.View
{
    public interface IBoardStepView
    {
        void ApplyMoveStep(BoardDelta delta, System.Action<BoardDelta> onCompleted);
        void ApplyFallStep(BoardDelta delta, System.Action<BoardDelta> onCompleted);
        void ApplyDestroyStep(BoardDelta delta, System.Action<BoardDelta> onCompleted);
    }
}