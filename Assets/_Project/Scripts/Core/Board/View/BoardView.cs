using System;
using System.Collections.Generic;
using DG.Tweening;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Board.Utils;
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
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Transform blocksRoot;
        [SerializeField] private Transform blocksPoolRoot;
        [SerializeField] private List<BlockViewEntry> blockPrefabs;

        [Header("Layout")] [SerializeField] private float baseCellSize = 1.0f;
        [SerializeField] private float minCameraSize = 10.0f;
        [SerializeField] private string boardSortingLayerName = "Board";

        private readonly Dictionary<int, BlockView> _blockViewsById = new(50);
        private readonly Dictionary<int, BlockView> _prefabsByType = new();

        private IBoardInput _boardInput;
        private BoardSize _boardSize;
        private BlockViewPool _blockViewPool;
        private int _viewVersion;
        private float _currentCellSize;
        private float _currentBlockScale = 1.0f;

        private void Awake()
        {
            CollectBlockPrefabsByType();
            _blockViewPool = new BlockViewPool(_prefabsByType, blocksPoolRoot);
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
            _viewVersion++;
            Clear();

            _boardSize = boardData.Size;
            AdjustBoardAndCamera();

            for (var y = 0; y < boardData.Height; y++)
            {
                for (var x = 0; x < boardData.Width; x++)
                {
                    var coord = new BoardCoordinates(x, y);
                    var cellData = boardData.GetCell(coord);

                    if (cellData.IsEmpty)
                        continue;

                    CreateBlockView(cellData, coord);
                }
            }
        }

        public void ApplyMoveStep(BoardDelta delta, Action<BoardDelta> onComplete)
        {
            var capturedViewVersion = _viewVersion;
            var completion = new TweenBatchCompletion(() => _viewVersion == capturedViewVersion, delta, onComplete);

            foreach (var item in delta.Items)
            {
                if (!item.IsMove)
                    continue;

                var blockId = item.CurrentCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out var blockView) || !blockView)
                {
                    Debug.LogError($"[BoardView] ApplyMoveStep: no BlockView for blockId={blockId}");
                    continue;
                }

                blockView.SetSorting(boardSortingLayerName, GetBlockSortingOrder(item.To));

                var targetPosition = GetBlockWorldPosition(item.To);
                var tween = blockView.PlayMove(targetPosition, MoveDuration);

                completion.RegisterTween(tween);
            }

            completion.CompleteIfEmpty();
        }

        public void ApplyFallStep(BoardDelta delta, Action<BoardDelta> onComplete)
        {
            var capturedViewVersion = _viewVersion;
            var completion = new TweenBatchCompletion(() => _viewVersion == capturedViewVersion, delta, onComplete);

            foreach (var item in delta.Items)
            {
                if (!item.IsMove)
                    continue;

                var blockId = item.CurrentCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out var blockView) || !blockView)
                {
                    Debug.LogError($"[BoardView] ApplyFallStep: no BlockView for blockId={blockId}");
                    continue;
                }

                blockView.SetSorting(boardSortingLayerName, GetBlockSortingOrder(item.To));

                var targetPosition = GetBlockWorldPosition(item.To);
                var currentPosition = blockView.transform.position;

                var distance = Vector3.Distance(currentPosition, targetPosition);

                if (distance <= 0.0f)
                {
                    blockView.SetPosition(targetPosition);
                    continue;
                }

                var duration = Mathf.Max(0.05f, distance / FallSpeed);
                var tween = blockView.PlayFall(targetPosition, duration);

                completion.RegisterTween(tween);
            }

            completion.CompleteIfEmpty();
        }

        public void ApplyDestroyStep(BoardDelta delta, Action<BoardDelta> onComplete)
        {
            var capturedViewVersion = _viewVersion;
            var completion = new TweenBatchCompletion(() => _viewVersion == capturedViewVersion, delta, onComplete);

            foreach (var item in delta.Items)
            {
                if (!item.IsDestroy)
                    continue;

                var blockId = item.PreviousCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out var blockView) || !blockView)
                {
                    Debug.LogError($"[BoardView] ApplyDestroyStep: no BlockView for blockId={blockId}");
                    continue;
                }

                var tween = blockView.PlayDestroy();

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

            var blockView = _blockViewPool.Get(cellData.BlockTypeId, blocksRoot);

            if (!blockView)
            {
                Debug.LogError($"Failed to get BlockView for BlockTypeId={cellData.BlockTypeId}");
                return;
            }

            blockView.Initialize(cellData.BlockTypeId, cellData.BlockId);
            blockView.transform.localScale = new Vector3(_currentBlockScale, _currentBlockScale, 1f);
            blockView.SetPosition(GetBlockWorldPosition(coord));
            blockView.SetSorting(boardSortingLayerName, GetBlockSortingOrder(coord));

            SubscribeToBlock(blockView);
            _blockViewsById.Add(cellData.BlockId, blockView);
        }

        private void AdjustBoardAndCamera()
        {
            var aspect = boardCamera.aspect;

            var backgroundBounds = backgroundRenderer.bounds;
            var bgBottom = backgroundBounds.min.y;
            var bgTop = backgroundBounds.max.y;
            var bgWidth = backgroundBounds.size.x;
            var bgHeight = backgroundBounds.size.y;

            var maxCameraSizeByHeight = bgHeight * 0.5f;
            var maxCameraSizeByWidth = bgWidth / (2f * aspect);
            var maxCameraSizeByBackground = Mathf.Min(maxCameraSizeByHeight, maxCameraSizeByWidth);

            var basePadding = baseCellSize;

            var requiredSizeForBaseWidth =
                (_boardSize.Width * baseCellSize + basePadding * 2f) / (2f * aspect);

            var requiredSizeForBaseHeight =
                (_boardSize.Height * baseCellSize + basePadding * 2f) * 0.5f;

            var requiredCameraSizeForBaseCell =
                Mathf.Max(minCameraSize, requiredSizeForBaseWidth, requiredSizeForBaseHeight);

            float finalCellSize;
            float targetCameraSize;

            if (requiredCameraSizeForBaseCell <= maxCameraSizeByBackground)
            {
                finalCellSize = baseCellSize;
                targetCameraSize = requiredCameraSizeForBaseCell;
            }
            else
            {
                var maxVisibleWidth = 2f * maxCameraSizeByBackground * aspect;
                var maxVisibleHeight = 2f * maxCameraSizeByBackground;

                var cellSizeByWidth = maxVisibleWidth / (_boardSize.Width + 2f);
                var cellSizeByHeight = maxVisibleHeight / (_boardSize.Height + 2f);

                finalCellSize = Mathf.Min(baseCellSize, cellSizeByWidth, cellSizeByHeight);
                finalCellSize = Mathf.Max(finalCellSize, 0.0001f);

                var finalPadding = finalCellSize;

                var sizeForWidth =
                    (_boardSize.Width * finalCellSize + finalPadding * 2f) / (2f * aspect);

                var sizeForHeight =
                    (_boardSize.Height * finalCellSize + finalPadding * 2f) * 0.5f;

                targetCameraSize = Mathf.Max(minCameraSize, sizeForWidth, sizeForHeight);
                targetCameraSize = Mathf.Min(targetCameraSize, maxCameraSizeByBackground);
            }

            _currentCellSize = finalCellSize;
            _currentBlockScale = finalCellSize / baseCellSize;

            blocksRoot.localScale = Vector3.one;

            var blocksRootPos = blocksRoot.position;
            blocksRootPos.x = -((_boardSize.Width - 1) * _currentCellSize * 0.5f);
            blocksRoot.position = blocksRootPos;

            var finalBoardHeight = _boardSize.Height * finalCellSize;
            var finalPaddingSize = finalCellSize;

            var boardBottomY = blocksRoot.position.y - finalCellSize * 0.5f;
            var boardTopY = boardBottomY + finalBoardHeight;

            var cameraTopIfAttachedToBottom = bgBottom + 2f * targetCameraSize;
            var requiredBoardTopWithPadding = boardTopY + finalPaddingSize;

            float cameraBottom;

            if (cameraTopIfAttachedToBottom >= requiredBoardTopWithPadding)
            {
                cameraBottom = bgBottom;
            }
            else
            {
                var desiredBottom = boardBottomY - finalPaddingSize;
                var maxCameraBottom = bgTop - 2f * targetCameraSize;
                cameraBottom = Mathf.Clamp(desiredBottom, bgBottom, maxCameraBottom);
            }

            boardCamera.orthographicSize = targetCameraSize;

            var cameraPos = boardCamera.transform.position;
            cameraPos.y = cameraBottom + targetCameraSize;
            boardCamera.transform.position = cameraPos;
        }

        private void Clear()
        {
            List<BlockView> blockViews = new(_blockViewsById.Values);
            _blockViewsById.Clear();

            foreach (var blockView in blockViews)
            {
                if (!blockView)
                    continue;

                UnsubscribeFromBlock(blockView);
                _blockViewPool.Release(blockView);
            }
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
            var blocksRootPosition = blocksRoot.position;

            return new Vector3(
                blocksRootPosition.x + coord.X * _currentCellSize,
                blocksRootPosition.y + coord.Y * _currentCellSize,
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
    }
}