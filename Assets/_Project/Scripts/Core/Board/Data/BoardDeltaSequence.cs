using System.Collections.Generic;

namespace OpenMyGame.Core.Board.Data
{
    public sealed class BoardDeltaSequence
    {
        private readonly List<BoardDelta> _steps = new();

        public IReadOnlyList<BoardDelta> Steps => _steps;

        public bool HasSteps => _steps.Count > 0;

        public void AddStep(BoardDelta delta)
        {
            if (delta == null)
                return;

            _steps.Add(delta);
        }

        public void Clear()
        {
            _steps.Clear();
        }
    }
}