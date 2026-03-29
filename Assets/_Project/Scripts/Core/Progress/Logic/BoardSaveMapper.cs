using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Progress.Data;

namespace OpenMyGame.Core.Progress.Logic
{
    public static class BoardSaveMapper
    {
        public static BoardSaveData ToSaveData(BoardData boardData)
        {
            int width = boardData.Width;
            int height = boardData.Height;

            CellSaveData[] cells = new CellSaveData[width * height];
            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CellData cellData = boardData.GetCell(new BoardCoordinates(x, y));

                    cells[index++] = new CellSaveData
                    {
                        isEmpty = cellData.IsEmpty,
                        blockTypeId = cellData.IsEmpty ? -1 : cellData.BlockTypeId,
                        blockId = cellData.IsEmpty ? -1 : cellData.BlockId
                    };
                }
            }

            return new BoardSaveData
            {
                width = width,
                height = height,
                cells = cells
            };
        }
    }
}