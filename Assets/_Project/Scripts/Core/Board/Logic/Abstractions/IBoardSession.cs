using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Level.Data;
using OpenMyGame.Core.Progress.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface IBoardSession
    {
        BoardData BoardData { get; }
        bool IsInitialized { get; }

        void Initialize(LevelConfigData levelConfigData);
        void Initialize(BoardSaveData boardSaveData);

        BoardDelta ApplyMoveStep(BoardMove move);
        BoardDelta BuildFallStep();
        BoardDelta BuildDestroyStep();
    }
}