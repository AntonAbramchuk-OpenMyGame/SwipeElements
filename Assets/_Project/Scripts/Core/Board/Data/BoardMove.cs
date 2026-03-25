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
    }
}