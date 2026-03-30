using System.Collections.Generic;
using UnityEngine;

namespace OpenMyGame.Core.Background
{
    public sealed class BalloonPool
    {
        private readonly BalloonView _prefab;
        private readonly Transform _activeRoot;
        private readonly Transform _poolRoot;
        private readonly Stack<BalloonView> _items = new();

        public BalloonPool(
            BalloonView prefab,
            Transform activeRoot,
            Transform poolRoot,
            int prewarmCount)
        {
            _prefab = prefab;
            _activeRoot = activeRoot;
            _poolRoot = poolRoot;

            for (var i = 0; i < prewarmCount; i++)
            {
                var item = CreateInstance();
                Return(item);
            }
        }

        public BalloonView Get()
        {
            var item = _items.Count > 0
                ? _items.Pop()
                : CreateInstance();

            item.transform.SetParent(_activeRoot, false);
            item.gameObject.SetActive(true);
            return item;
        }

        public void Return(BalloonView item)
        {
            if (!item)
                return;

            item.StopImmediate();
            item.gameObject.SetActive(false);
            item.transform.SetParent(_poolRoot, false);
            _items.Push(item);
        }

        private BalloonView CreateInstance()
        {
            return Object.Instantiate(_prefab, _poolRoot);
        }
    }
}