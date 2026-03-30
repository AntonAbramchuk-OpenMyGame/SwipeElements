using OpenMyGame.Core.Progress.Data;

namespace OpenMyGame.Core.Progress.Logic.Abstractions
{
    public interface IGameProgressService
    {
        void Initialize();
        int GetCompletedLevelsCount();
        LevelRunSnapshotData GetLevelRunSnapshot();
        void SaveCurrentRun(string levelId, BoardSaveData boardSaveData);
        void MarkLevelCompleted();
    }
}