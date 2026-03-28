using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Level.Data;

namespace OpenMyGame.Core.Board.Logic.Abstractions
{
    public interface IBoardFactory
    {
        BoardData CreateFromConfig(LevelConfigData levelConfigData);
    }
}