using System;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Level.Data;
using OpenMyGame.Core.Progress.Data;

namespace OpenMyGame.Core.Board.Logic
{
    public sealed class BoardFactory : IBoardFactory
    {
        public BoardData CreateFromConfig(LevelConfigData levelConfigData)
        {
            if (levelConfigData == null)
                throw new ArgumentNullException(nameof(levelConfigData));

            if (string.IsNullOrWhiteSpace(levelConfigData.LevelId))
                throw new ArgumentException("LevelId is null or empty.", nameof(levelConfigData));

            if (levelConfigData.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(levelConfigData.Width), "Width must be greater than 0.");

            if (levelConfigData.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(levelConfigData.Height), "Height must be greater than 0.");

            if (levelConfigData.Cells == null)
                throw new ArgumentNullException(nameof(levelConfigData.Cells));

            BoardSize size = new(levelConfigData.Width, levelConfigData.Height);

            if (levelConfigData.Cells.Count != size.CellCount)
            {
                throw new ArgumentException(
                    $"Cells length ({levelConfigData.Cells.Count}) does not match board size ({size.CellCount}).",
                    nameof(levelConfigData));
            }

            CellData[] cells = new CellData[levelConfigData.Cells.Count];
            int nextBlockId = 0;

            // LevelConfigData.Cells are defined top-to-bottom, like a visual picture.
            // BoardData uses bottom-to-top coordinates where y = 0 is the bottom row.
            // So we invert Y while copying config data into runtime board data.
            for (int i = 0; i < levelConfigData.Cells.Count; i++)
            {
                int x = i % size.Width;
                int yFromTop = i / size.Width;
                int y = size.Height - 1 - yFromTop;

                int runtimeIndex = y * size.Width + x;
                int blockTypeId = levelConfigData.Cells[i];

                if (blockTypeId < 0)
                {
                    cells[runtimeIndex] = CellData.Empty;
                }
                else
                {
                    cells[runtimeIndex] = CellData.CreateFilled(blockTypeId, nextBlockId);
                    nextBlockId++;
                }
            }

            return new BoardData(size, cells);
        }

        public BoardData CreateFromSave(BoardSaveData boardSaveData)
        {
            if (boardSaveData == null)
                throw new ArgumentNullException(nameof(boardSaveData));

            if (boardSaveData.width <= 0)
                throw new ArgumentOutOfRangeException(nameof(boardSaveData.width));

            if (boardSaveData.height <= 0)
                throw new ArgumentOutOfRangeException(nameof(boardSaveData.height));

            if (boardSaveData.cells == null)
                throw new ArgumentNullException(nameof(boardSaveData.cells));

            BoardSize size = new(boardSaveData.width, boardSaveData.height);

            if (boardSaveData.cells.Length != size.CellCount)
            {
                throw new ArgumentException(
                    $"Cells length ({boardSaveData.cells.Length}) does not match board size ({size.CellCount}).",
                    nameof(boardSaveData));
            }

            CellData[] cells = new CellData[size.CellCount];

            // ВАЖНО:
            // SaveData уже хранится в runtime-порядке (bottom → top),
            // т.е. в том же виде, как BoardData.
            // Поэтому НИКАКОЙ инверсии Y делать не нужно.

            for (int i = 0; i < boardSaveData.cells.Length; i++)
            {
                CellSaveData saveCell = boardSaveData.cells[i];

                if (saveCell.isEmpty)
                {
                    cells[i] = CellData.Empty;
                }
                else
                {
                    cells[i] = CellData.CreateFilled(
                        saveCell.blockTypeId,
                        saveCell.blockId);
                }
            }

            return new BoardData(size, cells);
        }
    }
}