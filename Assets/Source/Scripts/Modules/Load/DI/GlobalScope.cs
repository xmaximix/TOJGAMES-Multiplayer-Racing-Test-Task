using TojGamesTask.Common.DI;
using TojGamesTask.Common.Input;
using TojGamesTask.Common.Logging;
using TojGamesTask.Common.Networking;
using TojGamesTask.Common.SceneManagement;
using TojGamesTask.Modules.Load.Bootstrap;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using ILogger = TojGamesTask.Common.Logging.ILogger;

namespace TojGamesTask.Modules.Load.DI
{
    public class GlobalScope : BaseLifetimeScope
    {
        [SerializeField] private InputActionAsset inputAsset;
        [SerializeField] private PlayerAvatar playerAvatarPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            DontDestroyOnLoad(gameObject);

            base.Configure(builder);

            builder.RegisterInstance(inputAsset).As<InputActionAsset>();
            builder.Register<IInputService, UnityInputService>(Lifetime.Singleton);

            builder.RegisterInstance(playerAvatarPrefab)
                .As<PlayerAvatar>()
                .AsImplementedInterfaces();

            builder.Register<ILogger, UnityLogger>(Lifetime.Singleton);

            builder.Register<INetworkService, FusionNetworkService>(Lifetime.Singleton);
            builder.Register<ISceneService, UnitySceneService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<LoaderBootstrap>();
        }
    }
}