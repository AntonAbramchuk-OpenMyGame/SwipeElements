using System.Collections.Generic;
using UnityEngine;

namespace OpenMyGame.Core.Level.Data
{
    [System.Serializable]
    public sealed class LevelConfigData
    {
        [SerializeField] private string levelId;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private List<int> cells;

        public string LevelId => levelId;
        public int Width => width;
        public int Height => height;
        public IReadOnlyList<int> Cells => cells;

        public LevelConfigData(string levelId, int width, int height, List<int> cells)
        {
            this.levelId = levelId;
            this.width = width;
            this.height = height;
            this.cells = cells;
        }
    }
}