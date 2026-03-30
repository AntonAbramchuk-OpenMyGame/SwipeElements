using System;
using System.Collections.Generic;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Board.Utils;
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

        public BoardData BoardData => _boardSession.BoardData;

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

            BoardCoordinates target = move.GetTargetCoordinates();

            if (!BoardData.IsInside(target))
                return false;

            if (move.Direction == BoardMoveDirection.Up)
            {
                CellData targetCell = BoardData.GetCell(target);

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

            UnityEngine.Debug.Log(
                $"[Controller] TryAdvanceLogic | pending={_pendingMoves.Count}, move={_activeMoveCount}, fall={_activeFallCount}, destroy={_activeDestroyCount}"
            );

            ProcessPendingMoves();

            if (_activeMoveCount > 0)
                return;

            UnityEngine.Debug.Log("[Controller] About to try fall");
            TryStartFall();

            if (_activeMoveCount > 0 || _activeFallCount > 0)
                return;

            UnityEngine.Debug.Log("[Controller] About to try destroy");
            TryStartDestroy();

            if (_activeDestroyCount > 0)
                return;

            if (_activeMoveCount == 0 && _activeFallCount == 0)
            {
                UnityEngine.Debug.Log("[Controller] About to try extra fall");
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
                BoardMove move = _pendingMoves.Peek();
                _pendingMoves.Dequeue();

                BoardDelta moveDelta = _boardSession.ApplyMoveStep(move);

                if (!moveDelta.HasItems)
                    continue;

                UnityEngine.Debug.Log("New MoveDelta builded");

                ReserveDelta(moveDelta);
                _activeMoveCount++;

                PlayDelta(moveDelta, OnMoveCompleted);
            }
        }

        private void TryStartFall()
        {
            BoardDelta fallDelta = _boardSession.BuildFallStep();

            UnityEngine.Debug.Log("New FallDelta builded");
            BoardDebugPrinter.Print(BoardData);

            if (!fallDelta.HasItems)
            {
                UnityEngine.Debug.Log("FallDelta is empty");
                return;
            }

            ReserveDelta(fallDelta);
            _activeFallCount++;

            PlayDelta(fallDelta, OnFallCompleted);
        }

        private void TryStartDestroy()
        {
            BoardDebugPrinter.Print(BoardData);

            BoardDelta destroyDelta = _boardSession.BuildDestroyStep();

            UnityEngine.Debug.Log("New DestroyDelta builded");
            UnityEngine.Debug.Log(
                $"[Controller] DestroyDelta has items = {destroyDelta.HasItems}, count = {destroyDelta.Items.Count}"
            );
            BoardDebugPrinter.Print(BoardData);

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
            UnityEngine.Debug.Log("[Controller] LEVEL COMPLETED");
            LevelCompleted?.Invoke();

            return true;
        }

        private void OnMoveCompleted(BoardDelta delta)
        {
            UnityEngine.Debug.Log("[Controller] OnMoveCompleted");

            ReleaseDelta(delta);
            _activeMoveCount--;

            TryProcessPipeline();
        }

        private void OnFallCompleted(BoardDelta delta)
        {
            UnityEngine.Debug.Log("[Controller] OnFallCompleted");

            ReleaseDelta(delta);
            _activeFallCount--;

            TryProcessPipeline();
        }

        private void OnDestroyCompleted(BoardDelta delta)
        {
            UnityEngine.Debug.Log("[Controller] OnDestroyCompleted");

            ReleaseDelta(delta);
            _activeDestroyCount--;

            TryProcessPipeline();
        }

        private void ReserveDelta(BoardDelta delta)
        {
            for (int i = 0; i < delta.Items.Count; i++)
            {
                BoardDeltaItem item = delta.Items[i];

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
            for (int i = 0; i < delta.Items.Count; i++)
            {
                BoardDeltaItem item = delta.Items[i];

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
                int x = item.From.X;
                int minY = item.From.Y < item.To.Y ? item.From.Y : item.To.Y;
                int maxY = item.From.Y > item.To.Y ? item.From.Y : item.To.Y;

                for (int y = minY; y <= maxY; y++)
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
                int y = item.From.Y;
                int minX = item.From.X < item.To.X ? item.From.X : item.To.X;
                int maxX = item.From.X > item.To.X ? item.From.X : item.To.X;

                for (int x = minX; x <= maxX; x++)
                {
                    ReserveCell(new BoardCoordinates(x, y));
                }
            }
        }

        private void ReleaseFallPathWithSupport(BoardDeltaItem item)
        {
            if (item.From.X == item.To.X)
            {
                int x = item.From.X;
                int minY = item.From.Y < item.To.Y ? item.From.Y : item.To.Y;
                int maxY = item.From.Y > item.To.Y ? item.From.Y : item.To.Y;

                for (int y = minY; y <= maxY; y++)
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
                int y = item.From.Y;
                int minX = item.From.X < item.To.X ? item.From.X : item.To.X;
                int maxX = item.From.X > item.To.X ? item.From.X : item.To.X;

                for (int x = minX; x <= maxX; x++)
                {
                    ReleaseCell(new BoardCoordinates(x, y));
                }
            }
        }

        private bool IsReserved(BoardCoordinates coordinates)
        {
            return _reservedCellCounters.TryGetValue(coordinates, out int count) && count > 0;
        }

        private void ReserveCell(BoardCoordinates coordinates)
        {
            if (_reservedCellCounters.TryGetValue(coordinates, out int count))
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
            if (!_reservedCellCounters.TryGetValue(coordinates, out int count))
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
            UnityEngine.Debug.Log($"[Controller] PlayDelta: {delta.Type}, items: {delta.Items.Count}");

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