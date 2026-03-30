using System;

namespace OpenMyGame.Core.Progress.Data
{
    [Serializable]
    public sealed class LevelRunSnapshotData
    {
        public string levelId;
        public BoardSaveData board;
    }
}