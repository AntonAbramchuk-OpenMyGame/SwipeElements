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
            _gameHudView.NextClicked += OnNextClicked;
            _gameHudView.HideWinScreen();

            RunGuarded(InitializeCoreAsync);
        }

        public void Dispose()
        {
            DisposeBoardFlow();

            _gameHudView.RestartClicked -= OnRestartClicked;
            _gameHudView.NextClicked -= OnNextClicked;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        // ---------------------------------------------------------------------
        // Startup
        // ---------------------------------------------------------------------

        private async UniTask InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _levelProvider.InitializeAsync(cancellationToken);
            await _gameProgressService.InitializeAsync(cancellationToken);

            if (_gameProgressService.TryGetProgress(out GameProgressData progressData))
            {
                if (TryRestoreSavedRun(progressData))
                    return;

                await StartLevelByProgressAsync(progressData.completedLevelsCount, cancellationToken);
                return;
            }

            await StartLevelByProgressAsync(0, cancellationToken);
        }

        private bool TryRestoreSavedRun(GameProgressData progressData)
        {
            LevelRunSnapshotData snapshot = progressData.activeLevelSnapshot;

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

        // ---------------------------------------------------------------------
        // Level flow
        // ---------------------------------------------------------------------

        private async UniTask StartLevelByProgressAsync(int completedLevelsCount, CancellationToken cancellationToken)
        {
            var levelConfig =
                await _levelProvider.LoadLevelByCompletedLevelsCountAsync(completedLevelsCount, cancellationToken);

            _currentLevelId = levelConfig.LevelId;

            RecreateBoardFlow();
            _boardSession.Initialize(levelConfig);
            _boardView.Build(_boardSession.BoardData);

            await SaveCurrentRunAsync(completedLevelsCount, cancellationToken);
        }

        private async UniTask RestartCurrentLevelAsync(CancellationToken cancellationToken)
        {
            int completedLevelsCount = GetCompletedLevelsCount();

            if (string.IsNullOrWhiteSpace(_currentLevelId))
            {
                await StartLevelByProgressAsync(completedLevelsCount, cancellationToken);
                return;
            }

            var levelConfig = await _levelProvider.LoadLevelByIdAsync(_currentLevelId, cancellationToken);

            _gameHudView.HideWinScreen();

            RecreateBoardFlow();
            _boardSession.Initialize(levelConfig);
            _boardView.Build(_boardSession.BoardData);

            await SaveCurrentRunAsync(completedLevelsCount, cancellationToken);
        }

        private async UniTask CompleteLevelAsync(CancellationToken cancellationToken)
        {
            int completedLevelsCount = GetCompletedLevelsCount() + 1;

            GameProgressData progressData = new()
            {
                version = 1,
                completedLevelsCount = completedLevelsCount,
                activeLevelSnapshot = null
            };

            await _gameProgressService.SaveAsync(progressData, cancellationToken);
            _gameHudView.ShowWinScreen();
        }

        private async UniTask NextLevelAsync(CancellationToken cancellationToken)
        {
            _gameHudView.HideWinScreen();
            await StartLevelByProgressAsync(GetCompletedLevelsCount(), cancellationToken);
        }

        private UniTask SaveCurrentRunAsync(int completedLevelsCount, CancellationToken cancellationToken)
        {
            GameProgressData progressData = new()
            {
                version = 1,
                completedLevelsCount = completedLevelsCount,
                activeLevelSnapshot = new LevelRunSnapshotData
                {
                    levelId = _currentLevelId,
                    board = BoardSaveMapper.ToSaveData(_boardSession.BoardData)
                }
            };

            return _gameProgressService.SaveAsync(progressData, cancellationToken);
        }

        // ---------------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------------

        private void OnBoardSettled()
        {
            if (_isTransitionInProgress)
                return;

            RunGuarded(ct => SaveCurrentRunAsync(GetCompletedLevelsCount(), ct));
        }

        private void OnLevelCompleted()
        {
            RunGuarded(CompleteLevelAsync, useTransitionLock: true);
        }

        private void OnNextClicked()
        {
            RunGuarded(NextLevelAsync, useTransitionLock: true);
        }

        private void OnRestartClicked()
        {
            RunGuarded(RestartCurrentLevelAsync, useTransitionLock: true);
        }

        // ---------------------------------------------------------------------
        // Board flow lifecycle
        // ---------------------------------------------------------------------

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

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private int GetCompletedLevelsCount()
        {
            return _gameProgressService.TryGetProgress(out GameProgressData progressData)
                ? progressData.completedLevelsCount
                : 0;
        }

        private void RunGuarded(
            Func<CancellationToken, UniTask> action,
            bool useTransitionLock = false)
        {
            if (_cts == null)
                return;

            if (useTransitionLock && _isTransitionInProgress)
                return;

            RunGuardedInternal(action, useTransitionLock).Forget();
        }

        private async UniTaskVoid RunGuardedInternal(
            Func<CancellationToken, UniTask> action,
            bool useTransitionLock)
        {
            CancellationToken cancellationToken = _cts.Token;

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