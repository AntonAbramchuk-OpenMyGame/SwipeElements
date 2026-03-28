using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;

namespace OpenMyGame.Core.Board.Logic
{
    public class LevelWinCondition : ILevelWinCondition
    {
        public bool IsCompleted(BoardData boardData)
        {
            for (int y = 0; y < boardData.Height; y++)
            {
                for (int x = 0; x < boardData.Width; x++)
                {
                    CellData cell = boardData.GetCell(new BoardCoordinates(x, y));

                    if (!cell.IsEmpty)
                        return false;
                }
            }

            return true;
        }
    }
}