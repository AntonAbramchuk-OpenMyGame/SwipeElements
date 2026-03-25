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
        [SerializeField] private bool useNormalizeWithoutMove = true;

        private void Start()
        {
            IBoardNormalizer normalizer = new BoardNormalizer();
            IBoardService boardService = new BoardService(normalizer);
            IBoardFactory boardFactory = new BoardFactory();
            IBoardSession boardSession = new BoardSession(boardService, boardFactory);

            LevelConfigData levelConfigData = new()
            {
                LevelId = "debug_level_001",
                Width = 5,
                Height = 5,
                Cells = new[]
                {
                    // top
                    0, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1,
                    0, -1, -1, -1, -1,
                    0, 0, -1, -1, -1
                }
            };

            boardSession.Initialize(levelConfigData);

            Debug.Log("=== AFTER INIT ===");
            BoardDebugPrinter.Print(boardSession.BoardData);

            if (useNormalizeWithoutMove)
            {
                Debug.Log("=== NORMALIZE WITHOUT MOVE ===");

                BoardDeltaSequence sequence = boardSession.NormalizeWithoutMove();

                Debug.Log($"NormalizeWithoutMove steps count: {sequence.Steps.Count}");
                PrintSequence(sequence);

                Debug.Log("=== AFTER NORMALIZE ===");
                BoardDebugPrinter.Print(boardSession.BoardData);
            }
            else
            {
                Debug.Log("=== APPLY MOVE ===");

                BoardMove move = new(
                    new BoardCoordinates(2, 2),
                    BoardMoveDirection.Right);

                BoardDeltaSequence sequence = boardSession.ApplyMove(move);

                Debug.Log($"ApplyMove steps count: {sequence.Steps.Count}");
                PrintSequence(sequence);

                Debug.Log("=== AFTER MOVE ===");
                BoardDebugPrinter.Print(boardSession.BoardData);
            }
        }

        private static void PrintSequence(BoardDeltaSequence sequence)
        {
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
                                $"prev={item.PreviousCell.BlockTypeId}, " +
                                $"curr={item.CurrentCell.BlockTypeId}");
                            break;

                        default:
                            Debug.Log($"  Item {j}: Unknown");
                            break;
                    }
                }
            }
        }
    }
}