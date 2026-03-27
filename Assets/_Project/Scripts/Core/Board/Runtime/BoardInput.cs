using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Session;
using UnityEngine;

namespace OpenMyGame.Core.Board.Runtime
{
    public sealed class BoardInput : IBoardInput
    {
        private const float SwipeThresholdPixels = 30.0f;

        private readonly IBoardController _boardController;
        private readonly IBoardSession _boardSession;

        private bool _isDragging;
        private int _pressedBlockId;
        private Vector2 _pressScreenPosition;

        public BoardInput(IBoardController boardController, IBoardSession boardSession)
        {
            _boardController = boardController;
            _boardSession = boardSession;
        }

        public void OnBlockPointerDown(int blockId, Vector2 screenPosition)
        {
            _isDragging = true;
            _pressedBlockId = blockId;
            _pressScreenPosition = screenPosition;
        }

        public void OnBlockDrag(int blockId, Vector2 screenPosition)
        {
        }

        public void OnBlockPointerUp(int blockId, Vector2 screenPosition)
        {
            if (!_isDragging)
            {
                ResetState();
                return;
            }

            if (blockId != _pressedBlockId)
            {
                ResetState();
                return;
            }

            Vector2 delta = screenPosition - _pressScreenPosition;

            if (delta.magnitude < SwipeThresholdPixels)
            {
                ResetState();
                return;
            }

            int cellIndex = _boardSession.BoardData.IndexByID(_pressedBlockId);

            if (cellIndex < 0)
            {
                ResetState();
                return;
            }

            BoardCoordinates origin = _boardSession.BoardData.ToCoordinates(cellIndex);
            BoardMoveDirection direction = GetMoveDirection(delta);
            BoardMove move = new BoardMove(origin, direction);

            _boardController.EnqueueMove(move);

            ResetState();
        }

        private void ResetState()
        {
            _isDragging = false;
            _pressedBlockId = CellData.EmptyBlockId;
            _pressScreenPosition = default;
        }

        private static BoardMoveDirection GetMoveDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x >= 0.0f ? BoardMoveDirection.Right : BoardMoveDirection.Left;

            return delta.y >= 0.0f ? BoardMoveDirection.Up : BoardMoveDirection.Down;
        }
    }
}