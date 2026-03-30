using System.Threading;
using Cysharp.Threading.Tasks;
using OpenMyGame.Core.Level.Data;

namespace OpenMyGame.Core.Level.Logic.Abstractions
{
    public interface ILevelProvider
    {
        bool HasLevel(string levelId);
        UniTask InitializeAsync(CancellationToken cancellationToken);
        UniTask<LevelConfigData> LoadLevelByIdAsync(string levelId, CancellationToken cancellationToken);

        UniTask<LevelConfigData> LoadLevelByCompletedLevelsCountAsync(
            int completedLevelsCount,
            CancellationToken cancellationToken
        );
    }
}