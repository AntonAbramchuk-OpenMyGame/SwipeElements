using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;

namespace OpenMyGame.Core.Board.Logic
{
    public class LevelWinCondition : ILevelWinCondition
    {
        public bool IsCompleted(BoardData boardData)
        {
            for (var y = 0; y < boardData.Height; y++)
            {
                for (var x = 0; x < boardData.Width; x++)
                {
                    var cell = boardData.GetCell(new BoardCoordinates(x, y));

                    if (!cell.IsEmpty)
                        return false;
                }
            }

            return true;
        }
    }
}