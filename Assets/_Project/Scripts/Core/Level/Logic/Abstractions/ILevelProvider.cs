using System.Threading;
using Cysharp.Threading.Tasks;
using OpenMyGame.Core.Level.Data;

namespace OpenMyGame.Core.Level.Logic.Abstractions
{
    public interface ILevelProvider
    {
        int LevelsCount { get; }
        bool HasLevel(string levelId);
        UniTask InitializeAsync(CancellationToken cancellationToken);
        string GetLevelIdByCompletedLevelsCount(int completedLevelsCount);
        UniTask<LevelConfigData> LoadLevelByIdAsync(string levelId, CancellationToken cancellationToken);

        UniTask<LevelConfigData> LoadLevelByCompletedLevelsCountAsync(
            int completedLevelsCount,
            CancellationToken cancellationToken
        );
    }
}