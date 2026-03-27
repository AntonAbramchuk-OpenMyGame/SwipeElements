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

        [Header("Refs")] [SerializeField] private Transform blocksRoot;
        [SerializeField] private BlockView blockViewPrefab;

        [Header("Layout")] [SerializeField] private Vector2 boardOrigin = Vector2.zero;
        [SerializeField] private float cellSize = 1.0f;

        private readonly Dictionary<int, BlockView> _blockViewsById = new();

        private IBoardInput _boardInput;

        public void Construct(IBoardInput boardInput)
        {
            _boardInput = boardInput;
        }

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

        public void ApplyMoveStep(BoardDelta delta, Action<BoardDelta> onCompleted)
        {
            TweenBatchCompletion completion = new(delta, onCompleted);

            for (int i = 0; i < delta.Items.Count; i++)
            {
                BoardDeltaItem item = delta.Items[i];

                if (!item.IsMove)
                    continue;

                int blockId = item.CurrentCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out BlockView blockView))
                {
                    Debug.LogError($"[BoardView] ApplyMoveStep: no BlockView for blockId={blockId}");
                    continue;
                }

                Vector3 targetPosition = GetBlockWorldPosition(item.To);
                Tween tween = blockView.PlayMove(targetPosition, MoveDuration);

                completion.RegisterTween(tween);
            }

            completion.CompleteImmediatelyIfEmpty();
        }

        public void ApplyFallStep(BoardDelta delta, Action<BoardDelta> onCompleted)
        {
            TweenBatchCompletion completion = new(delta, onCompleted);

            for (int i = 0; i < delta.Items.Count; i++)
            {
                BoardDeltaItem item = delta.Items[i];

                if (!item.IsMove)
                    continue;

                int blockId = item.CurrentCell.BlockId;

                if (!_blockViewsById.TryGetValue(blockId, out BlockView blockView))
                {
                    Debug.LogError($"[BoardView] ApplyFallStep: no BlockView for blockId={blockId}");
                    continue;
                }

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

            completion.CompleteImmediatelyIfEmpty();
        }

        public void ApplyDestroyStep(BoardDelta delta, Action<BoardDelta> onCompleted)
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

        private void CreateBlockView(CellData cellData, BoardCoordinates coord)
        {
            if (_blockViewsById.ContainsKey(cellData.BlockId))
            {
                Debug.LogError($"Duplicate BlockView for blockId={cellData.BlockId}");
                return;
            }

            BlockView blockView = Instantiate(blockViewPrefab, blocksRoot);
            blockView.Initialize(cellData.BlockTypeId, cellData.BlockId);
            blockView.SetPosition(GetBlockWorldPosition(coord));

            SubscribeToBlock(blockView);
            _blockViewsById.Add(cellData.BlockId, blockView);
        }

        private void Clear()
        {
            foreach (var pair in _blockViewsById)
            {
                if (pair.Value)
                {
                    UnsubscribeFromBlock(pair.Value);
                    pair.Value.Release();
                }
            }

            _blockViewsById.Clear();
        }

        private void SubscribeToBlock(BlockView blockView)
        {
            blockView.PointerDownEvent += OnBlockPointerDown;
            blockView.DragEvent += OnBlockDrag;
            blockView.PointerUpEvent += OnBlockPointerUp;
        }

        private void UnsubscribeFromBlock(BlockView blockView)
        {
            blockView.PointerDownEvent -= OnBlockPointerDown;
            blockView.DragEvent -= OnBlockDrag;
            blockView.PointerUpEvent -= OnBlockPointerUp;
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
            return new Vector3(
                boardOrigin.x + coord.X * cellSize,
                boardOrigin.y + coord.Y * cellSize,
                0.0f
            );
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

            public void RegisterTween(Tween tween)
            {
                _remaining++;

                bool finished = false;

                void MarkFinished()
                {
                    if (finished)
                        return;

                    finished = true;
                    _remaining--;

                    if (_completed)
                        return;

                    if (_remaining > 0)
                        return;

                    _completed = true;
                    _onCompleted?.Invoke(_delta);
                }

                tween.OnComplete(MarkFinished);
                tween.OnKill(MarkFinished);
            }

            public void CompleteImmediatelyIfEmpty()
            {
                if (_completed)
                    return;

                if (_remaining > 0)
                    return;

                _completed = true;
                _onCompleted?.Invoke(_delta);
            }
        }
    }
}