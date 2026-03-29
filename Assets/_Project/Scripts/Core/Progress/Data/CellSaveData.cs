using System;

namespace OpenMyGame.Core.Progress.Data
{
    [Serializable]
    public sealed class CellSaveData
    {
        public bool isEmpty;
        public int blockTypeId;
        public int blockId;
    }
}