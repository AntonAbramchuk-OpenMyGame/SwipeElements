using System.Threading;
using Cysharp.Threading.Tasks;
using OpenMyGame.Core.Progress.Data;

namespace OpenMyGame.Core.Progress.Logic.Abstractions
{
    public interface IGameProgressService
    {
        UniTask InitializeAsync(CancellationToken cancellationToken);
        bool TryGetProgress(out GameProgressData progressData);
        UniTask SaveAsync(GameProgressData progressData, CancellationToken cancellationToken);
        UniTask ClearAsync(CancellationToken cancellationToken);
    }
}