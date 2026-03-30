using System;
using System.Collections.Generic;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Board.View.Abstractions;

namespace OpenMyGame.Core.Board.Logic
{
    public sealed class BoardController : IBoardController
    {
        public event Action Settled;
        public event Action LevelCompleted;

        private readonly IBoardSession _boardSession;
        private readonly ILevelWinCondition _levelWinCondition;
        private readonly IBoardStepView _boardStepView;

        private readonly Queue<BoardMove> _pendingMoves = new();
        private readonly Dictionary<BoardCoordinates, int> _reservedCellCounters = new();

        private int _activeMoveCount;
        private int _activeFallCount;
        private int _activeDestroyCount;
        private bool _isLevelCompleted;
        private bool _isDisposed;

        private BoardData BoardData => _boardSession.BoardData;

        public BoardController(
            IBoardSession boardSession,
            ILevelWinCondition levelWinCondition,
            IBoardStepView boardStepView
        )
        {
            _boardSession = boardSession;
            _levelWinCondition = levelWinCondition;
            _boardStepView = boardStepView;
        }

        public void Dispose()
        {
            _isDisposed = true;

            Settled = null;
            LevelCompleted = null;

            _pendingMoves.Clear();
            _reservedCellCounters.Clear();
        }

        public void EnqueueMove(BoardMove move)
        {
            if (_isDisposed)
                return;

            if (_isLevelCompleted)
                return;

            if (!IsMoveValid(move))
                return;

            _pendingMoves.Enqueue(move);

            TryProcessPipeline();
        }

        private bool IsMoveValid(BoardMove move)
        {
            if (!BoardData.IsInside(move.Origin))
                return false;

            var target = move.GetTargetCoordinates();

            if (!BoardData.IsInside(target))
                return false;

            if (move.Direction == BoardMoveDirection.Up)
            {
                var targetCell = BoardData.GetCell(target);

                if (targetCell.IsEmpty)
                    return false;
            }

            if (IsReserved(move.Origin))
                return false;

            if (IsReserved(target))
                return false;

            return true;
        }

        private void TryProcessPipeline()
        {
            if (_isDisposed)
                return;

            ProcessPendingMoves();

            if (_activeMoveCount > 0)
                return;

            TryStartFall();

            if (_activeMoveCount > 0 || _activeFallCount > 0)
                return;

            TryStartDestroy();

            if (_activeDestroyCount > 0)
                return;

            if (_activeMoveCount == 0 && _activeFallCount == 0)
            {
                TryStartFall();

                if (_activeFallCount > 0)
                    return;
            }

            if (TryHandleLevelCompleted())
                return;

            if (_pendingMoves.Count == 0 &&
                _activeMoveCount == 0 &&
                _activeFallCount == 0 &&
                _activeDestroyCount == 0)
            {
                Settled?.Invoke();
            }
        }

        private void ProcessPendingMoves()
        {
            while (_pendingMoves.Count > 0)
            {
                var move = _pendingMoves.Peek();
                _pendingMoves.Dequeue();

                var moveDelta = _boardSession.ApplyMoveStep(move);

                if (!moveDelta.HasItems)
                    continue;

                ReserveDelta(moveDelta);
                _activeMoveCount++;

                PlayDelta(moveDelta, OnMoveCompleted);
            }
        }

        private void TryStartFall()
        {
            var fallDelta = _boardSession.BuildFallStep();

            if (!fallDelta.HasItems)
                return;

            ReserveDelta(fallDelta);
            _activeFallCount++;

            PlayDelta(fallDelta, OnFallCompleted);
        }

        private void TryStartDestroy()
        {
            var destroyDelta = _boardSession.BuildDestroyStep();

            if (!destroyDelta.HasItems)
                return;

            ReserveDelta(destroyDelta);
            _activeDestroyCount++;

            PlayDelta(destroyDelta, OnDestroyCompleted);
        }

        private bool TryHandleLevelCompleted()
        {
            if (_isLevelCompleted ||
                _pendingMoves.Count > 0 ||
                _activeMoveCount > 0 ||
                _activeFallCount > 0 ||
                _activeDestroyCount > 0)
            {
                return false;
            }

            if (!_levelWinCondition.IsCompleted(BoardData))
                return false;

            _isLevelCompleted = true;
            LevelCompleted?.Invoke();

            return true;
        }

        private void OnMoveCompleted(BoardDelta delta)
        {
            ReleaseDelta(delta);
            _activeMoveCount--;

            TryProcessPipeline();
        }

        private void OnFallCompleted(BoardDelta delta)
        {
            ReleaseDelta(delta);
            _activeFallCount--;

            TryProcessPipeline();
        }

        private void OnDestroyCompleted(BoardDelta delta)
        {
            ReleaseDelta(delta);
            _activeDestroyCount--;

            TryProcessPipeline();
        }

        private void ReserveDelta(BoardDelta delta)
        {
            foreach (var item in delta.Items)
            {
                switch (item.Type)
                {
                    case BoardDeltaItemType.Move:
                        ReserveMoveItem(item, delta.Type);
                        break;

                    case BoardDeltaItemType.Destroy:
                        ReserveCell(item.From);
                        break;
                }
            }
        }

        private void ReleaseDelta(BoardDelta delta)
        {
            foreach (var item in delta.Items)
            {
                switch (item.Type)
                {
                    case BoardDeltaItemType.Move:
                        ReleaseMoveItem(item, delta.Type);
                        break;

                    case BoardDeltaItemType.Destroy:
                        ReleaseCell(item.From);
                        break;
                }
            }
        }

        private void ReserveMoveItem(BoardDeltaItem item, BoardDeltaType deltaType)
        {
            if (deltaType == BoardDeltaType.Move)
            {
                ReserveCell(item.From);
                ReserveCell(item.To);
                return;
            }

            if (deltaType == BoardDeltaType.Fall)
            {
                ReserveFallPathWithSupport(item);
            }
        }

        private void ReleaseMoveItem(BoardDeltaItem item, BoardDeltaType deltaType)
        {
            if (deltaType == BoardDeltaType.Move)
            {
                ReleaseCell(item.From);
                ReleaseCell(item.To);
                return;
            }

            if (deltaType == BoardDeltaType.Fall)
            {
                ReleaseFallPathWithSupport(item);
            }
        }

        private void ReserveFallPathWithSupport(BoardDeltaItem item)
        {
            if (item.From.X == item.To.X)
            {
                var x = item.From.X;
                var minY = item.From.Y < item.To.Y ? item.From.Y : item.To.Y;
                var maxY = item.From.Y > item.To.Y ? item.From.Y : item.To.Y;

                for (var y = minY; y <= maxY; y++)
                {
                    ReserveCell(new BoardCoordinates(x, y));
                }

                BoardCoordinates support = new(x, item.To.Y - 1);
                if (BoardData.IsInside(support))
                {
                    ReserveCell(support);
                }

                return;
            }

            if (item.From.Y == item.To.Y)
            {
                var y = item.From.Y;
                var minX = item.From.X < item.To.X ? item.From.X : item.To.X;
                var maxX = item.From.X > item.To.X ? item.From.X : item.To.X;

                for (var x = minX; x <= maxX; x++)
                {
                    ReserveCell(new BoardCoordinates(x, y));
                }
            }
        }

        private void ReleaseFallPathWithSupport(BoardDeltaItem item)
        {
            if (item.From.X == item.To.X)
            {
                var x = item.From.X;
                var minY = item.From.Y < item.To.Y ? item.From.Y : item.To.Y;
                var maxY = item.From.Y > item.To.Y ? item.From.Y : item.To.Y;

                for (var y = minY; y <= maxY; y++)
                {
                    ReleaseCell(new BoardCoordinates(x, y));
                }

                BoardCoordinates support = new(x, item.To.Y - 1);
                if (BoardData.IsInside(support))
                {
                    ReleaseCell(support);
                }

                return;
            }

            if (item.From.Y == item.To.Y)
            {
                var y = item.From.Y;
                var minX = item.From.X < item.To.X ? item.From.X : item.To.X;
                var maxX = item.From.X > item.To.X ? item.From.X : item.To.X;

                for (var x = minX; x <= maxX; x++)
                {
                    ReleaseCell(new BoardCoordinates(x, y));
                }
            }
        }

        private bool IsReserved(BoardCoordinates coordinates)
        {
            return _reservedCellCounters.TryGetValue(coordinates, out var count) && count > 0;
        }

        private void ReserveCell(BoardCoordinates coordinates)
        {
            if (_reservedCellCounters.TryGetValue(coordinates, out var count))
            {
                _reservedCellCounters[coordinates] = count + 1;
            }
            else
            {
                _reservedCellCounters.Add(coordinates, 1);
            }
        }

        private void ReleaseCell(BoardCoordinates coordinates)
        {
            if (!_reservedCellCounters.TryGetValue(coordinates, out var count))
                return;

            if (count <= 1)
            {
                _reservedCellCounters.Remove(coordinates);
            }
            else
            {
                _reservedCellCounters[coordinates] = count - 1;
            }
        }

        private void PlayDelta(BoardDelta delta, Action<BoardDelta> onComplete)
        {
            switch (delta.Type)
            {
                case BoardDeltaType.Move:
                    _boardStepView.ApplyMoveStep(delta, onComplete);
                    break;

                case BoardDeltaType.Fall:
                    _boardStepView.ApplyFallStep(delta, onComplete);
                    break;

                case BoardDeltaType.Destroy:
                    _boardStepView.ApplyDestroyStep(delta, onComplete);
                    break;
            }
        }
    }
}