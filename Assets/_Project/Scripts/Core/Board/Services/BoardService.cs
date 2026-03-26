using System;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Normalization;

namespace OpenMyGame.Core.Board.Services
{
    public sealed class BoardService : IBoardService
    {
        private readonly IBoardNormalizer _boardNormalizer;

        public BoardService(IBoardNormalizer boardNormalizer)
        {
            _boardNormalizer = boardNormalizer ?? throw new ArgumentNullException(nameof(boardNormalizer));
        }

        public BoardDelta SetCell(
            BoardData boardData,
            BoardCoordinates coordinates,
            CellData cellData)
        {
            if (boardData == null)
                throw new ArgumentNullException(nameof(boardData));

            if (!boardData.IsInside(coordinates))
                throw new ArgumentOutOfRangeException(nameof(coordinates), "Coordinates are outside board.");

            BoardDelta delta = new BoardDelta(BoardDeltaType.Unknown);

            CellData previousCell = boardData.GetCell(coordinates);
            boardData.SetCell(coordinates, cellData);

            if (previousCell != cellData)
            {
                delta.AddItem(BoardDeltaItem.CreateSet(coordinates, previousCell, cellData));
            }

            return delta;
        }

        public BoardDelta ApplyMoveStep(
            BoardData boardData,
            BoardMove move)
        {
            if (boardData == null)
                throw new ArgumentNullException(nameof(boardData));

            if (!IsMoveValid(boardData, move))
                return new BoardDelta(BoardDeltaType.Move);

            BoardDelta moveDelta = new(BoardDeltaType.Move);

            BoardCoordinates target = GetTargetCoordinates(move);

            CellData originCell = boardData.GetCell(move.Origin);
            CellData targetCell = boardData.GetCell(target);

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

            BoardCoordinates target = GetTargetCoordinates(move);

            if (!boardData.IsInside(target))
                return false;

            CellData originCell = boardData.GetCell(move.Origin);

            if (originCell.IsEmpty)
                return false;

            return true;
        }

        private static BoardCoordinates GetTargetCoordinates(BoardMove move)
        {
            return move.Direction switch
            {
                BoardMoveDirection.Up => new BoardCoordinates(move.Origin.X, move.Origin.Y + 1),
                BoardMoveDirection.Right => new BoardCoordinates(move.Origin.X + 1, move.Origin.Y),
                BoardMoveDirection.Down => new BoardCoordinates(move.Origin.X, move.Origin.Y - 1),
                BoardMoveDirection.Left => new BoardCoordinates(move.Origin.X - 1, move.Origin.Y),
                _ => throw new ArgumentOutOfRangeException(nameof(move), "Move direction is invalid.")
            };
        }
    }
}