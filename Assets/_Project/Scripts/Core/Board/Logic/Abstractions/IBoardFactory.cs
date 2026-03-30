using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Level.Data;
using OpenMyGame.Core.Progress.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface IBoardFactory
    {
        BoardData CreateFromConfig(LevelConfigData levelConfigData);
        BoardData CreateFromSave(BoardSaveData boardSaveData);
    }
}