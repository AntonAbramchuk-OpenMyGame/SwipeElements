using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenMyGame.Core.Board.View
{
    public sealed class BlockViewPool : IDisposable
    {
        private readonly IReadOnlyDictionary<int, BlockView> _prefabsByType;
        private readonly Dictionary<int, Stack<BlockView>> _poolByType = new();
        private readonly Transform _poolParent;

        public BlockViewPool(IReadOnlyDictionary<int, BlockView> prefabsByType, Transform poolParent = null)
        {
            _prefabsByType = prefabsByType;
            _poolParent = poolParent;

            if (!_poolParent)
            {
                _poolParent = new GameObject(nameof(BlockViewPool)).transform;
            }

            foreach (var pair in _prefabsByType)
            {
                _poolByType[pair.Key] = new Stack<BlockView>(20);
            }
        }

        public BlockView Get(int blockTypeId, Transform parent)
        {
            if (!_prefabsByType.TryGetValue(blockTypeId, out var prefab) || !prefab)
            {
                Debug.LogError($"[BlockViewPool] No prefab for BlockTypeId={blockTypeId}");
                return null;
            }

            if (_poolByType.TryGetValue(blockTypeId, out var pool) && pool.Count > 0)
            {
                var blockView = pool.Pop();

                if (!blockView)
                {
                    return Object.Instantiate(prefab, parent);
                }

                blockView.transform.SetParent(parent, false);
                blockView.gameObject.SetActive(true);

                return blockView;
            }

            return Object.Instantiate(prefab, parent);
        }

        public void Release(BlockView blockView)
        {
            if (!blockView)
                return;

            var blockTypeId = blockView.BlockTypeId;

            if (!_poolByType.TryGetValue(blockTypeId, out var pool) || pool == null)
            {
                Debug.LogError($"[BlockViewPool] No pool for BlockTypeId={blockTypeId}");

                Object.Destroy(blockView.gameObject);
                return;
            }

            blockView.Release();
            blockView.transform.SetParent(_poolParent, false);
            blockView.gameObject.SetActive(false);

            if (pool.Contains(blockView))
                Debug.LogError($"[BlockViewPool] BlockId={blockView.BlockId} already in pool");

            pool.Push(blockView);
        }

        public void Dispose()
        {
            foreach (var pool in _poolByType.Values)
            {
                while (pool.Count > 0)
                {
                    var blockView = pool.Pop();

                    if (blockView)
                    {
                        Object.Destroy(blockView.gameObject);
                    }
                }
            }

            _poolByType.Clear();
        }
    }
}