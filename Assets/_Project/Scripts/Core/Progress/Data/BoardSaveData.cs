using System;

namespace OpenMyGame.Core.Progress.Data
{
    [Serializable]
    public sealed class BoardSaveData
    {
        public int width;
        public int height;
        public CellSaveData[] cells;
    }
}