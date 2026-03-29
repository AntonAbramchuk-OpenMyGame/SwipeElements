using OpenMyGame.Core.Board.Logic;
using OpenMyGame.Core.Board.Logic.Abstractions;
using OpenMyGame.Core.Board.View;
using OpenMyGame.Core.Board.View.Abstractions;
using OpenMyGame.Core.Level.Logic;
using OpenMyGame.Core.Level.Logic.Abstractions;
using OpenMyGame.Core.Progress.Logic;
using OpenMyGame.Core.Progress.Logic.Abstractions;
using OpenMyGame.Core.UI;
using UnityEngine;
using Zenject;

namespace OpenMyGame.Core.Bootstrap
{
    public sealed class BoardSceneInstaller : MonoInstaller
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameHudView gameHudView;

        public override void InstallBindings()
        {
            BindSceneReferences();
            BindBoardServices();
            BindLevelServices();
            BindProgressServices();
            BindHud();
            BindEntryPoint();
        }

        private void BindSceneReferences()
        {
            Container.Bind<BoardView>().FromInstance(boardView).AsSingle();
            Container.Bind<IBoardStepView>().FromInstance(boardView).AsSingle();
        }

        private void BindBoardServices()
        {
            Container.Bind<IBoardFactory>().To<BoardFactory>().AsSingle();
            Container.Bind<IBoardNormalizer>().To<BoardNormalizer>().AsSingle();
            Container.Bind<ILevelWinCondition>().To<LevelWinCondition>().AsSingle();
            Container.Bind<IBoardService>().To<BoardService>().AsSingle();
        }

        private void BindLevelServices()
        {
            Container.Bind<ILevelProvider>().To<LevelProvider>().AsSingle();
        }

        private void BindProgressServices()
        {
            Container.Bind<IGameProgressService>().To<GameProgressService>().AsSingle();
        }

        private void BindHud()
        {
            Container.Bind<GameHudView>().FromInstance(gameHudView).AsSingle();
        }

        private void BindEntryPoint()
        {
            Container.BindInterfacesAndSelfTo<BoardGameRunner>().AsSingle();
        }
    }
}