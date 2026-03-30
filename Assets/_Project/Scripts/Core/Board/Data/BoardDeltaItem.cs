namespace OpenMyGame.Core.Board.Data
{
    public readonly struct BoardDeltaItem
    {
        public readonly BoardDeltaItemType Type;

        public readonly BoardCoordinates From;
        public readonly BoardCoordinates To;

        public readonly CellData PreviousCell;
        public readonly CellData CurrentCell;

        public bool IsMove => Type == BoardDeltaItemType.Move;
        public bool IsDestroy => Type == BoardDeltaItemType.Destroy;

        private BoardDeltaItem(
            BoardDeltaItemType type,
            BoardCoordinates from,
            BoardCoordinates to,
            CellData previousCell,
            CellData currentCell
        )
        {
            Type = type;
            From = from;
            To = to;
            PreviousCell = previousCell;
            CurrentCell = currentCell;
        }

        public static BoardDeltaItem CreateMove(
            BoardCoordinates from,
            BoardCoordinates to,
            CellData cellData
        )
        {
            return new BoardDeltaItem(
                BoardDeltaItemType.Move,
                from,
                to,
                cellData,
                cellData
            );
        }

        public static BoardDeltaItem CreateDestroy(
            BoardCoordinates at,
            CellData previousCell
        )
        {
            return new BoardDeltaItem(
                BoardDeltaItemType.Destroy,
                at,
                at,
                previousCell,
                CellData.Empty
            );
        }
    }
}