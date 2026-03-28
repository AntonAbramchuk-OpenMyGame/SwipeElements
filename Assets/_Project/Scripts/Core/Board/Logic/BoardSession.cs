using System;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Level.Data;

namespace OpenMyGame.Core.Board.Logic
{
    public sealed class BoardSession : IBoardSession
    {
        private readonly IBoardService _boardService;
        private readonly IBoardFactory _boardFactory;

        public BoardData BoardData { get; private set; }
        public bool IsInitialized => BoardData != null;

        public BoardSession(
            IBoardService boardService,
            IBoardFactory boardFactory
        )
        {
            _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
            _boardFactory = boardFactory ?? throw new ArgumentNullException(nameof(boardFactory));
        }

        public void Initialize(LevelConfigData levelConfigData)
        {
            BoardData = _boardFactory.CreateFromConfig(levelConfigData);
        }

        public BoardDelta ApplyMoveStep(BoardMove move)
        {
            EnsureInitialized();
            return _boardService.ApplyMoveStep(BoardData, move);
        }

        public BoardDelta BuildFallStep()
        {
            EnsureInitialized();
            return _boardService.BuildFallStep(BoardData);
        }

        public BoardDelta BuildDestroyStep()
        {
            EnsureInitialized();
            return _boardService.BuildDestroyStep(BoardData);
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("BoardSession is not initialized.");
        }
    }
}