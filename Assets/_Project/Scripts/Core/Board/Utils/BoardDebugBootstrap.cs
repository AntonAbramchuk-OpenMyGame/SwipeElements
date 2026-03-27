using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Initialization;
using OpenMyGame.Core.Board.Normalization;
using OpenMyGame.Core.Board.Runtime;
using OpenMyGame.Core.Board.Services;
using OpenMyGame.Core.Board.Session;
using OpenMyGame.Core.Board.View;
using OpenMyGame.Core.Level.Data;
using UnityEngine;

namespace OpenMyGame.Core.Board.Utils
{
    public sealed class BoardDebugBootstrap : MonoBehaviour
    {
        private readonly LevelConfigData _levelConfigData = new()
        {
            LevelId = "debug_level_001",
            Width = 4,
            Height = 6,
            Cells = new[]
            {
                -1, 1, -1, -1,
                1, 0, -1, -1,
                1, 1, -1, 1,
                0, 1, 0, 0,
                1, 0, 1, 1,
                1, 0, 1, 1,
            }
        };

        [SerializeField] private BoardView boardView;

        private IBoardNormalizer _boardNormalizer;
        private IBoardService _boardService;
        private IBoardFactory _boardFactory;
        private IBoardSession _boardSession;
        private IBoardController _boardController;
        private IBoardInput _boardInput;

        private void Start()
        {
            _boardNormalizer = new BoardNormalizer();
            _boardService = new BoardService(_boardNormalizer);
            _boardFactory = new BoardFactory();
            _boardSession = new BoardSession(_boardService, _boardFactory);
            _boardController = new BoardController(_boardSession, boardView);
            _boardInput = new BoardInput(_boardController, _boardSession);
            boardView.Construct(_boardInput);

            _boardSession.Initialize(_levelConfigData);
            boardView.Build(_boardSession.BoardData);

            Debug.Log("=== AFTER INIT ===");
            BoardDebugPrinter.Print(_boardSession.BoardData);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _boardController.EnqueueMove(
                    new BoardMove(new BoardCoordinates(1, 4), BoardMoveDirection.Down)
                );

                Debug.Log("=== AFTER CONTROLLER LOGIC ===");
                BoardDebugPrinter.Print(_boardSession.BoardData);
            }

            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _boardController.EnqueueMove(
                    new BoardMove(new BoardCoordinates(0, 2), BoardMoveDirection.Right)
                );

                Debug.Log("=== AFTER CONTROLLER LOGIC ===");
                BoardDebugPrinter.Print(_boardSession.BoardData);
            }
        }
    }
}