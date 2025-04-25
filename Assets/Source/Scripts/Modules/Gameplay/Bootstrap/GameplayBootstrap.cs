using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using TojGamesTask.Common.Networking;
using TojGamesTask.Modules.Gameplay.Core;
using TojGamesTask.Modules.Gameplay.Factories;
using VContainer;
using VContainer.Unity;

namespace TojGamesTask.Modules.Gameplay
{
    public sealed class GameplayBootstrap : IStartable
    {
        private readonly INetworkService network;
        private readonly CarFactory factory;
        private readonly RaceSystem race;
        private readonly CountdownService countdown;

        [Inject]
        public GameplayBootstrap(
            INetworkService network,
            CarFactory factory,
            RaceSystem race,
            CountdownService countdown)
        {
            this.network = network;
            this.factory = factory;
            this.race = race;
            this.countdown = countdown;
        }

        public void Start()
        {
            _ = InitializeAsync();
        }

        private async UniTask InitializeAsync()
        {
            await UniTask.WaitUntil(() =>
                network.Runner != null &&
                network.Runner.State == NetworkRunner.States.Running
            );

            var players = network.Runner.ActivePlayers.ToArray();

            race.Init(players);

            if (network.IsHost)
                await factory.SpawnAllAsync(players);

            await UniTask.WaitUntil(() =>
                players.All(pr => network.Runner.TryGetPlayerObject(pr, out _))
            );

            await countdown.Run();
        }
    }
}