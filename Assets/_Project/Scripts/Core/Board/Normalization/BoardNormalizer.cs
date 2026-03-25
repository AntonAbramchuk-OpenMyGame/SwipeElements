using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Normalization
{
    public sealed class BoardNormalizer : IBoardNormalizer
    {
        public BoardDeltaSequence Normalize(BoardData boardData)
        {
            BoardDeltaSequence sequence = new();

            // Здесь позже появится pipeline по ТЗ:
            // Fall -> Destroy -> Fall -> Destroy -> ...

            return sequence;
        }
    }
}