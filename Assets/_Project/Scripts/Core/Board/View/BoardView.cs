using System.Collections.Generic;
using OpenMyGame.Core.Board.Data;
using UnityEngine;

namespace OpenMyGame.Core.Board.View
{
    public sealed class BoardView : MonoBehaviour, IBoardStepView
    {
        [Header("Refs")] [SerializeField] private Transform blocksRoot;
        [SerializeField] private BlockView blockViewPrefab;

        [Header("Layout")] [SerializeField] private Vector2 boardOrigin = Vector2.zero;
        [SerializeField] private float cellSize = 1.0f;

        private readonly Dictionary<int, BlockView> _blockViewsById = new();

        public int ActiveBlockViewCount => _blockViewsById.Count;

        public void Build(BoardData boardData)
        {
            Clear();

            for (int y = 0; y < boardData.Height; y++)
            {
                for (int x = 0; x < boardData.Width; x++)
                {
                    var coord = new BoardCoordinates(x, y);
                    var cellData = boardData[coord];

                    if (cellData.IsEmpty)
                        continue;

                    CreateBlockView(cellData, coord);
                }
            }
        }

        public void ApplyMoveStep(BoardDelta delta, System.Action<BoardDelta> onCompleted)
        {
            foreach (BoardDeltaItem item in delta.Items)
            {
                int blockId = item.CurrentCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out BlockView blockView))
                {
                    Debug.LogError($"[BoardView] ApplyMoveStep: no BlockView for blockId={blockId}");
                    continue;
                }

                blockView.SetPosition(GetWorldPosition(item.To));
            }

            onCompleted?.Invoke(delta);
        }

        public void ApplyFallStep(BoardDelta delta, System.Action<BoardDelta> onCompleted)
        {
            foreach (BoardDeltaItem item in delta.Items)
            {
                int blockId = item.CurrentCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out BlockView blockView))
                {
                    Debug.LogError($"[BoardView] ApplyFallStep: no BlockView for blockId={blockId}");
                    continue;
                }

                blockView.SetPosition(GetWorldPosition(item.To));
            }

            onCompleted?.Invoke(delta);
        }

        public void ApplyDestroyStep(BoardDelta delta, System.Action<BoardDelta> onCompleted)
        {
            foreach (BoardDeltaItem item in delta.Items)
            {
                int blockId = item.PreviousCell.BlockId;

                // ReSharper disable once CanSimplifyDictionaryRemovingWithSingleCall;
                if (!_blockViewsById.TryGetValue(blockId, out BlockView blockView))
                {
                    Debug.LogError($"[BoardView] ApplyDestroyStep: no BlockView for blockId={blockId}");
                    continue;
                }

                _blockViewsById.Remove(blockId);
                blockView.PlayDestroy();
            }

            onCompleted?.Invoke(delta);
        }

        public bool TryGetBlockView(int blockId, out BlockView blockView)
        {
            return _blockViewsById.TryGetValue(blockId, out blockView);
        }

        private void CreateBlockView(CellData cellData, BoardCoordinates coord)
        {
            if (_blockViewsById.ContainsKey(cellData.BlockId))
            {
                Debug.LogError($"Duplicate BlockView for blockId={cellData.BlockId}");
                return;
            }

            BlockView blockView = Instantiate(blockViewPrefab, blocksRoot);
            blockView.Initialize(cellData.BlockTypeId, cellData.BlockId);
            blockView.SetPosition(GetWorldPosition(coord));

            _blockViewsById.Add(cellData.BlockId, blockView);
        }

        private void Clear()
        {
            foreach (var pair in _blockViewsById)
            {
                if (pair.Value)
                    Destroy(pair.Value.gameObject);
            }

            _blockViewsById.Clear();
        }

        private Vector3 GetWorldPosition(BoardCoordinates coord)
        {
            return new Vector3(
                boardOrigin.x + coord.X * cellSize,
                boardOrigin.y + coord.Y * cellSize,
                0f);
        }
    }
}