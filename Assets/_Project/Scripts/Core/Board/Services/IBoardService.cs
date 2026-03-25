using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Services
{
    public interface IBoardService
    {
        BoardDelta SetCell(
            BoardData boardData,
            BoardCoordinates coordinates,
            CellData cellData
        );

        BoardDeltaSequence ApplyMove(
            BoardData boardData,
            BoardMove move
        );
    }
}