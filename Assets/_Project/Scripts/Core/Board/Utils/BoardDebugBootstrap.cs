using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Initialization;
using OpenMyGame.Core.Board.Normalization;
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
            // Dependencies
            IBoardNormalizer normalizer = new BoardNormalizer();
            IBoardService boardService = new BoardService(normalizer);
            IBoardFactory boardFactory = new BoardFactory();
            IBoardSession boardSession = new BoardSession(boardService, boardFactory);

            // --- INIT FROM CONFIG ---
            LevelConfigData levelConfigData = new()
            {
                LevelId = "debug_level_001",
                Width = 5,
                Height = 5,
                Cells = new[]
                {
                    // top
                    -1, -1, 0, -1, -1,
                    -1, -1, -1, -1, -1,
                    -1, -1, 1, -1, -1,
                    -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1
                    // bottom
                }
            };

            boardSession.Initialize(levelConfigData);

            Debug.Log("=== AFTER INIT ===");
            BoardDebugPrinter.Print(boardSession.BoardData);

            // --- APPLY MOVE ---
            BoardMove move = new(
                new BoardCoordinates(2, 4),
                BoardMoveDirection.Down);

            BoardDeltaSequence sequence = boardSession.ApplyMove(move);

            Debug.Log($"ApplyMove steps count: {sequence.Steps.Count}");

            // Лог дельты
            for (int i = 0; i < sequence.Steps.Count; i++)
            {
                BoardDelta step = sequence.Steps[i];
                Debug.Log($"Step {i}: {step.Type}, items: {step.Items.Count}");

                for (int j = 0; j < step.Items.Count; j++)
                {
                    BoardDeltaItem item = step.Items[j];

                    switch (item.Type)
                    {
                        case BoardDeltaItemType.Move:
                            Debug.Log(
                                $"  Item {j}: Move, " +
                                $"from=({item.From.X}, {item.From.Y}), " +
                                $"to=({item.To.X}, {item.To.Y}), " +
                                $"cell={item.PreviousCell.BlockTypeId}");
                            break;

                        case BoardDeltaItemType.Set:
                            Debug.Log(
                                $"  Item {j}: Set, " +
                                $"at=({item.From.X}, {item.From.Y}), " +
                                $"prev={item.PreviousCell.BlockTypeId}, " +
                                $"curr={item.CurrentCell.BlockTypeId}");
                            break;

                        case BoardDeltaItemType.Destroy:
                            Debug.Log(
                                $"  Item {j}: Destroy, " +
                                $"at=({item.From.X}, {item.From.Y}), " +
                                $"prev={item.PreviousCell.BlockTypeId}");
                            break;
                    }
                }
            }

            Debug.Log("=== AFTER MOVE ===");
            BoardDebugPrinter.Print(boardSession.BoardData);
        }
    }
}