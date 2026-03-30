using System;

namespace OpenMyGame.Core.Progress.Data
{
    [Serializable]
    public sealed class GameProgressData
    {
        public int completedLevelsCount;
        public LevelRunSnapshotData activeLevelSnapshot;
    }
}