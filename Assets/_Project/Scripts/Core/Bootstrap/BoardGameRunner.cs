using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Board.View;
using OpenMyGame.Core.Level.Data;
using OpenMyGame.Core.Level.Logic.Abstractions;
using UnityEngine;
using Zenject;

namespace OpenMyGame.Core.Bootstrap
{
    public sealed class BoardGameRunner : IInitializable, IDisposable
    {
        private readonly ILevelProvider _levelProvider;
        private readonly IBoardSession _boardSession;
        private readonly IBoardInput _boardInput;
        private readonly BoardView _boardView;

        private CancellationTokenSource _cts;

        public BoardGameRunner(
            ILevelProvider levelProvider,
            IBoardSession boardSession,
            IBoardInput boardInput,
            BoardView boardView
        )
        {
            _levelProvider = levelProvider;
            _boardSession = boardSession;
            _boardInput = boardInput;
            _boardView = boardView;
        }

        public void Initialize()
        {
            _cts = new CancellationTokenSource();
            InitializeAsync(_cts.Token).Forget();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InitializeCoreAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async UniTask InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _levelProvider.InitializeAsync(cancellationToken);

            LevelConfigData levelConfigData = await _levelProvider.LoadCurrentLevelAsync(cancellationToken);

            _boardSession.Initialize(levelConfigData);
            _boardView.Reinit(_boardInput);
            _boardView.Build(_boardSession.BoardData);
        }
    }
}