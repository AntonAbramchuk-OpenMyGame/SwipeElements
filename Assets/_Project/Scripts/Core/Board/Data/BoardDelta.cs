using System.Collections.Generic;

namespace OpenMyGame.Core.Board.Data
{
    public sealed class BoardDelta
    {
        private readonly List<BoardDeltaItem> _items = new();

        public readonly BoardDeltaType Type;

        public IReadOnlyList<BoardDeltaItem> Items => _items;
        public bool HasItems => _items.Count > 0;

        public BoardDelta(BoardDeltaType type)
        {
            Type = type;
        }

        public void AddItem(BoardDeltaItem item)
        {
            _items.Add(item);
        }
    }
}