using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using OpenMyGame.Core.Board.Logic;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Board.View;
using OpenMyGame.Core.Level.Logic.Abstractions;
using OpenMyGame.Core.Progress.Data;
using OpenMyGame.Core.Progress.Logic;
using OpenMyGame.Core.Progress.Logic.Abstractions;
using OpenMyGame.Core.UI;
using UnityEngine;
using Zenject;

namespace OpenMyGame.Core.Bootstrap
{
    public sealed class BoardGameRunner : IInitializable, IDisposable
    {
        private readonly ILevelProvider _levelProvider;
        private readonly IBoardFactory _boardFactory;
        private readonly IBoardService _boardService;
        private readonly ILevelWinCondition _levelWinCondition;
        private readonly IGameProgressService _gameProgressService;
        private readonly BoardView _boardView;
        private readonly GameHudView _gameHudView;

        private IBoardSession _boardSession;
        private IBoardController _boardController;
        private IBoardInput _boardInput;

        private CancellationTokenSource _cts;
        private bool _isTransitionInProgress;
        private string _currentLevelId;

        public BoardGameRunner(
            ILevelProvider levelProvider,
            IBoardFactory boardFactory,
            IBoardService boardService,
            ILevelWinCondition levelWinCondition,
            IGameProgressService gameProgressService,
            BoardView boardView,
            GameHudView gameHudView
        )
        {
            _levelProvider = levelProvider;
            _boardFactory = boardFactory;
            _boardService = boardService;
            _levelWinCondition = levelWinCondition;
            _gameProgressService = gameProgressService;
            _boardView = boardView;
            _gameHudView = gameHudView;
        }

        public void Initialize()
        {
            _cts = new CancellationTokenSource();

            _gameHudView.RestartClicked += OnRestartClicked;
            _gameHudView.SkipClicked += OnSkipClicked;

            RunGuarded(InitializeCoreAsync, true);
        }

        public void Dispose()
        {
            DisposeBoardFlow();

            _gameHudView.RestartClicked -= OnRestartClicked;
            _gameHudView.SkipClicked -= OnSkipClicked;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTask InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _levelProvider.InitializeAsync(cancellationToken);
            _gameProgressService.Initialize();

            var levelRunSnapshot = _gameProgressService.GetLevelRunSnapshot();

            if (TryRestoreSavedRun(levelRunSnapshot))
                return;

            var completedLevelsCount = _gameProgressService.GetCompletedLevelsCount();
            await StartLevelByProgressAsync(completedLevelsCount, cancellationToken);
        }

        private bool TryRestoreSavedRun(LevelRunSnapshotData snapshot)
        {
            if (snapshot == null)
                return false;

            if (!_levelProvider.HasLevel(snapshot.levelId))
            {
                Debug.LogWarning(
                    $"[BoardGameRunner] Saved snapshot level '{snapshot.levelId}' is missing in catalog. " +
                    "Falling back to completedLevelsCount."
                );

                return false;
            }

            _currentLevelId = snapshot.levelId;

            RecreateBoardFlow();
            _boardSession.Initialize(snapshot.board);
            _boardView.Build(_boardSession.BoardData);

            return true;
        }

        private async UniTask StartLevelByProgressAsync(int completedLevelsCount, CancellationToken cancellationToken)
        {
            var levelConfig =
                await _levelProvider.LoadLevelByCompletedLevelsCountAsync(completedLevelsCount, cancellationToken);

            _currentLevelId = levelConfig.LevelId;

            RecreateBoardFlow();
            _boardSession.Initialize(levelConfig);
            _boardView.Build(_boardSession.BoardData);

            SaveCurrentRun();
        }

        private async UniTask RestartCurrentLevelAsync(CancellationToken cancellationToken)
        {
            var completedLevelsCount = _gameProgressService.GetCompletedLevelsCount();

            if (string.IsNullOrWhiteSpace(_currentLevelId))
            {
                await StartLevelByProgressAsync(completedLevelsCount, cancellationToken);
                return;
            }

            var levelConfig = await _levelProvider.LoadLevelByIdAsync(_currentLevelId, cancellationToken);

            RecreateBoardFlow();
            _boardSession.Initialize(levelConfig);
            _boardView.Build(_boardSession.BoardData);

            SaveCurrentRun();
        }

        private async UniTask NextLevelAsync(CancellationToken cancellationToken)
        {
            _gameProgressService.MarkLevelCompleted();
            await StartLevelByProgressAsync(_gameProgressService.GetCompletedLevelsCount(), cancellationToken);
        }

        private void SaveCurrentRun()
        {
            _gameProgressService.SaveCurrentRun(
                _currentLevelId,
                BoardSaveMapper.ToSaveData(_boardSession.BoardData)
            );
        }

        private void OnBoardSettled()
        {
            if (_isTransitionInProgress)
                return;

            SaveCurrentRun();
        }

        private void OnLevelCompleted()
        {
            RunGuarded(NextLevelAsync, true);
        }

        private void OnRestartClicked()
        {
            RunGuarded(RestartCurrentLevelAsync, true);
        }

        private void OnSkipClicked()
        {
            RunGuarded(NextLevelAsync, true);
        }

        private void RecreateBoardFlow()
        {
            DisposeBoardFlow();

            _boardSession = new BoardSession(_boardService, _boardFactory);
            _boardController = new BoardController(_boardSession, _levelWinCondition, _boardView);
            _boardInput = new BoardInput(_boardController, _boardSession);

            _boardController.Settled += OnBoardSettled;
            _boardController.LevelCompleted += OnLevelCompleted;

            _boardView.Reinit(_boardInput);
        }

        private void DisposeBoardFlow()
        {
            if (_boardController != null)
            {
                _boardController.Settled -= OnBoardSettled;
                _boardController.LevelCompleted -= OnLevelCompleted;
                _boardController.Dispose();
                _boardController = null;
            }

            _boardSession = null;
            _boardInput = null;
        }

        private void RunGuarded(
            Func<CancellationToken, UniTask> action,
            bool useTransitionLock = false
        )
        {
            if (_cts == null)
                return;

            if (useTransitionLock && _isTransitionInProgress)
                return;

            RunGuardedInternal(action, useTransitionLock).Forget();
        }

        private async UniTaskVoid RunGuardedInternal(
            Func<CancellationToken, UniTask> action,
            bool useTransitionLock
        )
        {
            var cancellationToken = _cts.Token;

            if (useTransitionLock)
                _isTransitionInProgress = true;

            try
            {
                await action.Invoke(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (useTransitionLock)
                    _isTransitionInProgress = false;
            }
        }
    }
}