using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Normalization
{
    public sealed class BoardNormalizer : IBoardNormalizer
    {
        public BoardDeltaSequence Normalize(BoardData boardData)
        {
            BoardDeltaSequence sequence = new();

            BoardDelta fallDelta = ApplyFall(boardData);
            if (fallDelta.HasItems)
            {
                sequence.AddStep(fallDelta);
            }

            return sequence;
        }

        private static BoardDelta ApplyFall(BoardData boardData)
        {
            BoardDelta delta = new(BoardDeltaType.Fall);

            int width = boardData.Size.Width;
            int height = boardData.Size.Height;

            for (int x = 0; x < width; x++)
            {
                ApplyFallToColumn(boardData, x, height, delta);
            }

            return delta;
        }

        private static void ApplyFallToColumn(
            BoardData boardData,
            int x,
            int height,
            BoardDelta delta)
        {
            int targetY = 0;

            for (int y = 0; y < height; y++)
            {
                BoardCoordinates from = new(x, y);
                CellData cell = boardData.GetCell(from);

                if (cell.IsEmpty)
                    continue;

                if (y != targetY)
                {
                    BoardCoordinates to = new(x, targetY);

                    boardData.SetCell(to, cell);
                    boardData.SetCell(from, CellData.Empty);

                    delta.AddItem(BoardDeltaItem.CreateMove(from, to, cell));
                }

                targetY++;
            }
        }
    }
}