using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Initialization;
using OpenMyGame.Core.Board.Normalization;
using OpenMyGame.Core.Board.Runtime;
using OpenMyGame.Core.Board.Services;
using OpenMyGame.Core.Board.Session;
using OpenMyGame.Core.Level.Data;
using UnityEngine;

namespace OpenMyGame.Core.Board.Utils
{
    public sealed class BoardDebugBootstrap : MonoBehaviour
    {
        private void Start()
        {
            IBoardNormalizer normalizer = new BoardNormalizer();
            IBoardService boardService = new BoardService(normalizer);
            IBoardFactory boardFactory = new BoardFactory();
            IBoardSession boardSession = new BoardSession(boardService, boardFactory);

            BoardController boardController = new(boardSession);

            LevelConfigData levelConfigData = new()
            {
                LevelId = "debug_level_001",
                Width = 5,
                Height = 5,
                Cells = new[]
                {
                    // top -> bottom
                    -1, -1, 1, -1, -1,
                    -1, 1, 1, 1, -1,
                    -1, -1, 1, -1, -1,
                    -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1
                }
            };

            boardSession.Initialize(levelConfigData);

            Debug.Log("=== AFTER INIT ===");
            BoardDebugPrinter.Print(boardSession.BoardData);

            boardController.EnqueueMove(
                new BoardMove(new BoardCoordinates(2, 2), BoardMoveDirection.Right));

            Debug.Log("=== AFTER CONTROLLER LOGIC ===");
            BoardDebugPrinter.Print(boardSession.BoardData);
        }
    }
}