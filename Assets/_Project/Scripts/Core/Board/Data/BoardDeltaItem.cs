namespace OpenMyGame.Core.Board.Data
{
    public readonly struct BoardDeltaItem
    {
        public readonly BoardDeltaItemType Type;

        public readonly BoardCoordinates From;
        public readonly BoardCoordinates To;

        public readonly CellData PreviousCell;
        public readonly CellData CurrentCell;

        private BoardDeltaItem(
            BoardDeltaItemType type,
            BoardCoordinates from,
            BoardCoordinates to,
            CellData previousCell,
            CellData currentCell)
        {
            Type = type;
            From = from;
            To = to;
            PreviousCell = previousCell;
            CurrentCell = currentCell;
        }

        public bool IsMove => Type == BoardDeltaItemType.Move;
        public bool IsSet => Type == BoardDeltaItemType.Set;
        public bool IsDestroy => Type == BoardDeltaItemType.Destroy;

        public static BoardDeltaItem CreateMove(
            BoardCoordinates from,
            BoardCoordinates to,
            CellData cellData)
        {
            return new BoardDeltaItem(
                BoardDeltaItemType.Move,
                from,
                to,
                cellData,
                cellData);
        }

        public static BoardDeltaItem CreateSet(
            BoardCoordinates at,
            CellData previousCell,
            CellData currentCell)
        {
            return new BoardDeltaItem(
                BoardDeltaItemType.Set,
                at,
                at,
                previousCell,
                currentCell);
        }

        public static BoardDeltaItem CreateDestroy(
            BoardCoordinates at,
            CellData previousCell)
        {
            return new BoardDeltaItem(
                BoardDeltaItemType.Destroy,
                at,
                at,
                previousCell,
                CellData.Empty);
        }
    }
}