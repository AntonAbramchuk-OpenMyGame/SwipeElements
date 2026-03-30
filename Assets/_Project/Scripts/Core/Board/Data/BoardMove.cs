namespace OpenMyGame.Core.Board.Data
{
    public readonly struct BoardMove
    {
        public readonly BoardCoordinates Origin;
        public readonly BoardMoveDirection Direction;

        public BoardMove(BoardCoordinates origin, BoardMoveDirection direction)
        {
            Origin = origin;
            Direction = direction;
        }

        public BoardCoordinates GetTargetCoordinates()
        {
            return Direction switch
            {
                BoardMoveDirection.Up => new BoardCoordinates(Origin.X, Origin.Y + 1),
                BoardMoveDirection.Right => new BoardCoordinates(Origin.X + 1, Origin.Y),
                BoardMoveDirection.Down => new BoardCoordinates(Origin.X, Origin.Y - 1),
                BoardMoveDirection.Left => new BoardCoordinates(Origin.X - 1, Origin.Y),
                _ => Origin
            };
        }
    }
}