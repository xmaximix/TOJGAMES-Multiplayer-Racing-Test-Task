using TojGamesTask.Common.DI;
using TojGamesTask.Common.Input;
using TojGamesTask.Common.Logging;
using TojGamesTask.Common.Networking;
using TojGamesTask.Common.SceneManagement;
using TojGamesTask.Modules.Lobby.Domain;
using TojGamesTask.Modules.Lobby.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using ILogger = TojGamesTask.Common.Logging.ILogger;

namespace TojGamesTask.Modules.Lobby.DI
{
    public class LobbyScope : BaseLifetimeScope
    {
        [SerializeField] private LobbyView _lobbyView;
        [SerializeField] private PlayerAvatar playerAvatarPrefab;
        [SerializeField] private InputActionAsset _inputAsset;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            
            builder.RegisterInstance(_inputAsset);
            builder.Register<IInputService, UnityInputService>(Lifetime.Singleton);

            builder.RegisterInstance(playerAvatarPrefab);
            
            builder.Register<ILogger, UnityLogger>(Lifetime.Singleton);
            
            builder.Register<INetworkService, FusionNetworkService>(Lifetime.Singleton);
            builder.Register<ISceneService, UnitySceneService>(Lifetime.Singleton);
            builder.Register<ILobbySystem, LobbySystem>(Lifetime.Singleton);
            
            builder.RegisterComponent(_lobbyView);
            builder.Register<LobbyPresenter>(Lifetime.Singleton).As<IInitializable>();
        }
    }
}