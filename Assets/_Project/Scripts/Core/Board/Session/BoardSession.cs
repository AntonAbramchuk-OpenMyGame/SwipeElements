using System;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Initialization;
using OpenMyGame.Core.Board.Services;
using OpenMyGame.Core.Level.Data;

namespace OpenMyGame.Core.Board.Session
{
    public sealed class BoardSession : IBoardSession
    {
        private readonly IBoardService _boardService;
        private readonly IBoardFactory _boardFactory;

        public BoardData BoardData { get; private set; }

        public bool IsInitialized => BoardData != null;

        public BoardSession(
            IBoardService boardService,
            IBoardFactory boardFactory)
        {
            _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
            _boardFactory = boardFactory ?? throw new ArgumentNullException(nameof(boardFactory));
        }

        public void Initialize(BoardSize size)
        {
            BoardData = _boardFactory.CreateEmpty(size);
        }

        public void Initialize(LevelConfigData levelConfigData)
        {
            BoardData = _boardFactory.CreateFromConfig(levelConfigData);
        }

        public BoardDelta SetCell(
            BoardCoordinates coordinates,
            CellData cellData)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("BoardSession is not initialized.");

            return _boardService.SetCell(BoardData, coordinates, cellData);
        }

        public BoardDeltaSequence ApplyMove(BoardMove move)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("BoardSession is not initialized.");

            return _boardService.ApplyMove(BoardData, move);
        }

        public BoardDeltaSequence NormalizeWithoutMove()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("BoardSession is not initialized.");

            return _boardService.NormalizeWithoutMove(BoardData);
        }
    }
}