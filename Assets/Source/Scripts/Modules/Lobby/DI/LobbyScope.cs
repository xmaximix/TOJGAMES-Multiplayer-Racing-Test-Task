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
        [Header("Lobby UI")]
        [SerializeField] private LobbyView lobbyView;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.Register<ILobbySystem, LobbySystem>(Lifetime.Singleton);

            builder.RegisterComponent(lobbyView);

            builder.RegisterEntryPoint<LobbyPresenter>();
        }
    }
}