using System.Text;
using OpenMyGame.Core.Board.Data;
using UnityEngine;

namespace OpenMyGame.Core.Board.Utils
{
    // ReSharper disable once UnusedType.Global;
    public static class BoardDebugPrinter
    {
        // ReSharper disable once UnusedMember.Global;
        public static void Print(BoardData boardData)
        {
            if (boardData == null)
            {
                Debug.Log("Board is null");
                return;
            }

            int width = boardData.Size.Width;
            int height = boardData.Size.Height;

            StringBuilder sb = new();

            sb.AppendLine("=== BOARD (top -> bottom) ===");

            // Верхняя рамка
            sb.Append("     ");
            sb.AppendLine(new string('-', width * 3));

            // Основное поле
            for (int y = height - 1; y >= 0; y--)
            {
                sb.Append($"{y,3} |");

                for (int x = 0; x < width; x++)
                {
                    int value = boardData.GetCell(new BoardCoordinates(x, y)).BlockTypeId;

                    if (value < 0)
                        sb.Append("  .");
                    else
                        sb.Append($"{value,3}");
                }

                sb.AppendLine();
            }

            // Нижняя рамка
            sb.Append("     ");
            sb.AppendLine(new string('-', width * 3));

            // Ось X снизу
            sb.Append("     ");
            for (int x = 0; x < width; x++)
            {
                sb.Append($"{x,3}");
            }

            sb.AppendLine();

            Debug.Log(sb.ToString());
        }
    }
}