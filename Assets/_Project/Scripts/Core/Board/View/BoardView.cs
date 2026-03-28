using System;
using System.Collections.Generic;
using DG.Tweening;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Board.View.Abstractions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenMyGame.Core.Board.View
{
    public sealed class BoardView : MonoBehaviour, IBoardStepView
    {
        private const float MoveDuration = 0.3f;
        private const float FallSpeed = 8.0f;

        [Header("Refs")] [SerializeField] private Camera boardCamera;
        [SerializeField] private Transform blocksRoot;
        [SerializeField] private List<BlockViewEntry> blockPrefabs;

        [Header("Layout")] [SerializeField] private float cellSize = 1.0f;
        [SerializeField] private float boardWidthPadding = 2.0f;
        [SerializeField] private float fixedCameraBottomY = -11.3f;
        [SerializeField] private float minCameraSize = 10.0f;
        [SerializeField] private string boardSortingLayerName = "Board";

        private readonly Dictionary<int, BlockView> _blockViewsById = new(50);
        private readonly Dictionary<int, BlockView> _prefabsByType = new();

        private IBoardInput _boardInput;
        private BoardSize _boardSize;
        private BlockViewPool _blockViewPool;

        private void Awake()
        {
            CollectBlockPrefabsByType();
            _blockViewPool = new BlockViewPool(_prefabsByType);
        }

        private void OnDestroy()
        {
            _blockViewPool?.Dispose();
        }

        public void Reinit(IBoardInput boardInput)
        {
            _boardInput = boardInput;
        }

        public void Build(BoardData boardData)
        {
            Clear();

            _boardSize = boardData.Size;

            for (int y = 0; y < boardData.Height; y++)
            {
                for (int x = 0; x < boardData.Width; x++)
                {
                    var coord = new BoardCoordinates(x, y);
                    var cellData = boardData.GetCell(coord);

                    if (cellData.IsEmpty)
                        continue;

                    CreateBlockView(cellData, coord);
                }
            }

            AdjustBoardAndCamera();
        }

        public void ApplyMoveStep(BoardDelta delta, Action<BoardDelta> onCompleted)
        {
            TweenBatchCompletion completion = new(delta, onCompleted);

            foreach (var item in delta.Items)
            {
                if (!item.IsMove)
                    continue;

                int blockId = item.CurrentCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out BlockView blockView) || !blockView)
                {
                    Debug.LogError($"[BoardView] ApplyMoveStep: no BlockView for blockId={blockId}");
                    continue;
                }

                blockView.SetSorting(boardSortingLayerName, GetBlockSortingOrder(item.To));

                Vector3 targetPosition = GetBlockWorldPosition(item.To);
                Tween tween = blockView.PlayMove(targetPosition, MoveDuration);

                completion.RegisterTween(tween);
            }

            completion.CompleteIfEmpty();
        }

        public void ApplyFallStep(BoardDelta delta, Action<BoardDelta> onCompleted)
        {
            TweenBatchCompletion completion = new(delta, onCompleted);

            foreach (var item in delta.Items)
            {
                if (!item.IsMove)
                    continue;

                int blockId = item.CurrentCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out BlockView blockView) || !blockView)
                {
                    Debug.LogError($"[BoardView] ApplyFallStep: no BlockView for blockId={blockId}");
                    continue;
                }

                blockView.SetSorting(boardSortingLayerName, GetBlockSortingOrder(item.To));

                Vector3 targetPosition = GetBlockWorldPosition(item.To);
                Vector3 currentPosition = blockView.transform.position;

                float distance = Vector3.Distance(currentPosition, targetPosition);

                if (distance <= 0.0f)
                {
                    blockView.SetPosition(targetPosition);
                    continue;
                }

                float duration = Mathf.Max(0.05f, distance / FallSpeed);
                Tween tween = blockView.PlayFall(targetPosition, duration);

                completion.RegisterTween(tween);
            }

            completion.CompleteIfEmpty();
        }

        public void ApplyDestroyStep(BoardDelta delta, Action<BoardDelta> onCompleted)
        {
            TweenBatchCompletion completion = new(delta, onCompleted);

            foreach (var item in delta.Items)
            {
                if (!item.IsDestroy)
                    continue;

                int blockId = item.PreviousCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out BlockView blockView) || !blockView)
                {
                    Debug.LogError($"[BoardView] ApplyDestroyStep: no BlockView for blockId={blockId}");
                    continue;
                }

                Tween tween = blockView.PlayDestroy();

                completion.RegisterTween(tween, Release);
                continue;

                void Release()
                {
                    UnsubscribeFromBlock(blockView);
                    _blockViewsById.Remove(blockId);
                    _blockViewPool.Release(blockView);
                }
            }

            completion.CompleteIfEmpty();
        }

        private void CollectBlockPrefabsByType()
        {
            foreach (var entry in blockPrefabs)
            {
                if (_prefabsByType.ContainsKey(entry.BlockTypeId))
                {
                    Debug.LogError($"Duplicate prefab for type {entry.BlockTypeId}");
                    continue;
                }

                _prefabsByType.Add(entry.BlockTypeId, entry.Prefab);
            }
        }

        private void CreateBlockView(CellData cellData, BoardCoordinates coord)
        {
            if (_blockViewsById.ContainsKey(cellData.BlockId))
            {
                Debug.LogError($"Duplicate BlockView for blockId={cellData.BlockId}");
                return;
            }

            BlockView blockView = _blockViewPool.Get(cellData.BlockTypeId, blocksRoot);

            if (!blockView)
            {
                Debug.LogError($"Failed to get BlockView for BlockTypeId={cellData.BlockTypeId}");
                return;
            }

            blockView.Initialize(cellData.BlockTypeId, cellData.BlockId);
            blockView.SetPosition(GetBlockWorldPosition(coord));
            blockView.SetSorting(boardSortingLayerName, GetBlockSortingOrder(coord));

            SubscribeToBlock(blockView);
            _blockViewsById.Add(cellData.BlockId, blockView);
        }

        private void AdjustBoardAndCamera()
        {
            Vector3 blocksRootPos = blocksRoot.position;
            blocksRootPos.x = -((_boardSize.Width - 1) * cellSize * 0.5f);
            blocksRoot.position = blocksRootPos;

            float boardWidth = _boardSize.Width * cellSize;
            float boardHeight = _boardSize.Height * cellSize;

            float boardBottomY = blocksRoot.position.y - cellSize * 0.5f;
            float cameraBottomY = Mathf.Min(boardBottomY, fixedCameraBottomY);
            float bottomMargin = boardBottomY - cameraBottomY;

            float sizeForHeight = (boardHeight + bottomMargin * 2) * 0.5f;
            float sizeForWidth = (boardWidth + boardWidthPadding * 2) / (2 * boardCamera.aspect);

            float targetSize = Mathf.Max(minCameraSize, sizeForHeight, sizeForWidth);
            boardCamera.orthographicSize = targetSize;

            Vector3 cameraPos = boardCamera.transform.position;
            cameraPos.y = cameraBottomY + targetSize;
            boardCamera.transform.position = cameraPos;
        }

        private void Clear()
        {
            foreach (var pair in _blockViewsById)
            {
                if (pair.Value)
                {
                    UnsubscribeFromBlock(pair.Value);
                    _blockViewPool.Release(pair.Value);
                }
            }

            _blockViewsById.Clear();
        }

        private void SubscribeToBlock(BlockView blockView)
        {
            if (blockView)
            {
                blockView.PointerDownEvent += OnBlockPointerDown;
                blockView.DragEvent += OnBlockDrag;
                blockView.PointerUpEvent += OnBlockPointerUp;
            }
        }

        private void UnsubscribeFromBlock(BlockView blockView)
        {
            if (blockView)
            {
                blockView.PointerDownEvent -= OnBlockPointerDown;
                blockView.DragEvent -= OnBlockDrag;
                blockView.PointerUpEvent -= OnBlockPointerUp;
            }
        }

        private void OnBlockPointerDown(int blockId, PointerEventData eventData)
        {
            _boardInput?.OnBlockPointerDown(blockId, eventData.position);
        }

        private void OnBlockDrag(int blockId, PointerEventData eventData)
        {
            _boardInput?.OnBlockDrag(blockId, eventData.position);
        }

        private void OnBlockPointerUp(int blockId, PointerEventData eventData)
        {
            _boardInput?.OnBlockPointerUp(blockId, eventData.position);
        }

        private Vector3 GetBlockWorldPosition(BoardCoordinates coord)
        {
            Vector3 blocksRootPosition = blocksRoot.position;

            return new Vector3(
                blocksRootPosition.x + coord.X * cellSize,
                blocksRootPosition.y + coord.Y * cellSize,
                0.0f
            );
        }

        private int GetBlockSortingOrder(BoardCoordinates coord)
        {
            return coord.Y * _boardSize.Width + coord.X;
        }

        [Serializable]
        public class BlockViewEntry
        {
            [SerializeField] private int blockTypeId;
            [SerializeField] private BlockView prefab;

            public int BlockTypeId => blockTypeId;
            public BlockView Prefab => prefab;
        }

        private sealed class TweenBatchCompletion
        {
            private readonly BoardDelta _delta;
            private readonly Action<BoardDelta> _onCompleted;

            private int _remaining;
            private bool _completed;

            public TweenBatchCompletion(BoardDelta delta, Action<BoardDelta> onCompleted)
            {
                _delta = delta;
                _onCompleted = onCompleted;
            }

            public void RegisterTween(Tween tween, Action onCompleteOrKill = null)
            {
                _remaining++;

                bool completedOrKilled = false;

                void OnCompleteOrKill()
                {
                    if (completedOrKilled)
                        return;

                    completedOrKilled = true;
                    _remaining--;

                    onCompleteOrKill?.Invoke();

                    if (_remaining > 0 || _completed)
                        return;

                    _completed = true;
                    _onCompleted?.Invoke(_delta);
                }

                tween.OnComplete(OnCompleteOrKill);
                tween.OnKill(OnCompleteOrKill);
            }

            public void CompleteIfEmpty()
            {
                if (_remaining > 0 || _completed)
                    return;

                _completed = true;
                _onCompleted?.Invoke(_delta);
            }
        }
    }
}