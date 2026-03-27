using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Level.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface IBoardSession
    {
        BoardData BoardData { get; }
        bool IsInitialized { get; }

        void Initialize(BoardSize size);
        void Initialize(LevelConfigData levelConfigData);

        BoardDelta SetCell(
            BoardCoordinates coordinates,
            CellData cellData
        );

        BoardDelta ApplyMoveStep(BoardMove move);

        BoardDelta BuildFallStep();

        BoardDelta BuildDestroyStep();
    }
}