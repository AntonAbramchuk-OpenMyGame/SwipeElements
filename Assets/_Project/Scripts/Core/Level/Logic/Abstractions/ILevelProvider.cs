using System.Threading;
using Cysharp.Threading.Tasks;
using OpenMyGame.Core.Level.Data;

namespace OpenMyGame.Core.Level.Logic.Abstractions
{
    public interface ILevelProvider
    {
        UniTask InitializeAsync(CancellationToken cancellationToken);
        UniTask<LevelConfigData> LoadCurrentLevelAsync(CancellationToken cancellationToken);
        UniTask<LevelConfigData> LoadNextLevelAsync(CancellationToken cancellationToken);
    }
}