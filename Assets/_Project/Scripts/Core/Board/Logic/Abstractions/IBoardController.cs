using System;
using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface IBoardController
    {
        event Action Settled;
        event Action LevelCompleted;
        void EnqueueMove(BoardMove move);
        public void Dispose();
    }
}