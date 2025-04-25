using Fusion;
using TojGamesTask.Common.DI;
using TojGamesTask.Common.Input;
using TojGamesTask.Common.Logging;
using TojGamesTask.Common.Networking;
using TojGamesTask.Common.SceneManagement;
using TojGamesTask.Modules.Gameplay.Configs;
using TojGamesTask.Modules.Gameplay.Core;
using TojGamesTask.Modules.Gameplay.Network;
using TojGamesTask.Modules.Gameplay.Factories;
using TojGamesTask.Modules.Gameplay.Presentation.Presenters;
using TojGamesTask.Modules.Gameplay.Presentation.Views;
using TojGamesTask.Modules.Gameplay.Presenters;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using ILogger = TojGamesTask.Common.Logging.ILogger;

namespace TojGamesTask.Modules.Gameplay.DI
{
    public class GameplayScope : BaseLifetimeScope
    {
        [SerializeField] private TrackScript trackScript;
        [SerializeField] private NetworkObject carPrefab;

        [Header("UI Views")]
        [SerializeField] private CountdownView countdownView;
        [SerializeField] private HUDView hudView;
        [SerializeField] private FinishView finishView;

        [Header("Configs")]
        [SerializeField] private CarConfig carConfig;
        [SerializeField] private CountdownConfig countdownConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInstance(trackScript).AsImplementedInterfaces();
            builder.RegisterInstance(carPrefab).As<NetworkObject>();

            builder.RegisterComponent(countdownView);
            builder.RegisterComponent(hudView);
            builder.RegisterComponent(finishView);

            builder.RegisterInstance(carConfig).As<CarConfig>();
            builder.RegisterInstance(countdownConfig).As<CountdownConfig>();

            builder.Register<CarFactory>(Lifetime.Scoped);
            builder.Register<RaceSystem>(Lifetime.Scoped);
            builder.Register<CountdownService>(Lifetime.Scoped);

            builder.RegisterEntryPoint<GameplayBootstrap>();
            builder.RegisterEntryPoint<CountdownPresenter>();
            builder.RegisterEntryPoint<HUDPresenter>();
            builder.RegisterEntryPoint<FinishPresenter>();
        }
    }
}