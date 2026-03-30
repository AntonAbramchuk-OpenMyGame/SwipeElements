using System;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;

namespace OpenMyGame.Core.Board.Logic
{
    public sealed class BoardService : IBoardService
    {
        private readonly IBoardNormalizer _boardNormalizer;

        public BoardService(IBoardNormalizer boardNormalizer)
        {
            _boardNormalizer = boardNormalizer ?? throw new ArgumentNullException(nameof(boardNormalizer));
        }

        public BoardDelta ApplyMoveStep(
            BoardData boardData,
            BoardMove move
        )
        {
            if (boardData == null)
                throw new ArgumentNullException(nameof(boardData));

            BoardDelta moveDelta = new(BoardDeltaType.Move);

            if (!IsMoveValid(boardData, move))
                return moveDelta;

            var target = move.GetTargetCoordinates();

            var originCell = boardData.GetCell(move.Origin);
            var targetCell = boardData.GetCell(target);

            boardData.SetCell(move.Origin, targetCell);
            boardData.SetCell(target, originCell);

            if (!originCell.IsEmpty)
            {
                moveDelta.AddItem(BoardDeltaItem.CreateMove(move.Origin, target, originCell));
            }

            if (!targetCell.IsEmpty)
            {
                moveDelta.AddItem(BoardDeltaItem.CreateMove(target, move.Origin, targetCell));
            }

            return moveDelta;
        }

        public BoardDelta BuildFallStep(BoardData boardData)
        {
            if (boardData == null)
                throw new ArgumentNullException(nameof(boardData));

            return _boardNormalizer.BuildFallStep(boardData);
        }

        public BoardDelta BuildDestroyStep(BoardData boardData)
        {
            if (boardData == null)
                throw new ArgumentNullException(nameof(boardData));

            return _boardNormalizer.BuildDestroyStep(boardData);
        }

        private static bool IsMoveValid(BoardData boardData, BoardMove move)
        {
            if (!boardData.IsInside(move.Origin))
                return false;

            var target = move.GetTargetCoordinates();

            if (!boardData.IsInside(target))
                return false;

            var originCell = boardData.GetCell(move.Origin);

            if (originCell.IsEmpty)
                return false;

            return true;
        }
    }
}