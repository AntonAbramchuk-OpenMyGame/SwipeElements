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

        public BlockViewPool(IReadOnlyDictionary<int, BlockView> prefabsByType)
        {
            _prefabsByType = prefabsByType;

            foreach (var pair in _prefabsByType)
            {
                _poolByType[pair.Key] = new Stack<BlockView>(20);
            }
        }

        public BlockView Get(int blockTypeId, Transform parent)
        {
            if (!_prefabsByType.TryGetValue(blockTypeId, out BlockView prefab) || !prefab)
            {
                Debug.LogError($"[BlockViewPool] No prefab for BlockTypeId={blockTypeId}");
                return null;
            }

            if (_poolByType.TryGetValue(blockTypeId, out Stack<BlockView> pool) && pool.Count > 0)
            {
                BlockView blockView = pool.Pop();

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

            int blockTypeId = blockView.BlockTypeId;

            if (!_poolByType.TryGetValue(blockTypeId, out Stack<BlockView> pool))
            {
                Debug.LogError($"[BlockViewPool] No pool for BlockTypeId={blockTypeId}");

                Object.Destroy(blockView.gameObject);
                return;
            }

            blockView.Release();
            blockView.transform.SetParent(null, false);
            blockView.gameObject.SetActive(false);

            pool.Push(blockView);
        }

        public void Dispose()
        {
            foreach (Stack<BlockView> pool in _poolByType.Values)
            {
                while (pool.Count > 0)
                {
                    BlockView blockView = pool.Pop();

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