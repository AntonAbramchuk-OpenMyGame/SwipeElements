using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Progress.Data;

namespace OpenMyGame.Core.Progress.Logic
{
    public static class BoardSaveMapper
    {
        public static BoardSaveData ToSaveData(BoardData boardData)
        {
            var width = boardData.Width;
            var height = boardData.Height;

            var cells = new CellSaveData[width * height];
            var index = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cellData = boardData.GetCell(new BoardCoordinates(x, y));

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