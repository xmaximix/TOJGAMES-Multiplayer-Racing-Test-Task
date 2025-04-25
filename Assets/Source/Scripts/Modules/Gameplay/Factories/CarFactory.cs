using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using TojGamesTask.Common.Networking;
using TojGamesTask.Modules.Gameplay.Configs;
using TojGamesTask.Modules.Gameplay.Core;
using TojGamesTask.Modules.Gameplay.Network;
using UnityEngine;
using VContainer;

namespace TojGamesTask.Modules.Gameplay.Factories
{
    public sealed class CarFactory
    {
        private readonly TrackScript track;
        private readonly INetworkService networkService;
        private readonly CarConfig carConfig;
        private readonly NetworkObject carPrefab;

        [Inject]
        public CarFactory(
            TrackScript track,
            INetworkService networkService,
            CarConfig carConfig,
            NetworkObject carPrefab)
        {
            this.track = track;
            this.networkService = networkService;
            this.carConfig = carConfig;
            this.carPrefab = carPrefab;
        }

        public async UniTask SpawnAllAsync(PlayerRef[] players)
        {
            if (!networkService.IsHost)
                return;

            var runner = networkService.Runner;
            var spawnPoints = track.SpawnPoints;
            var pointCount = spawnPoints.Count;

            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var position = spawnPoints[i % pointCount].transform.position;
                
                networkService.Nicknames.TryGetValue(player, out var nick);
                if (string.IsNullOrEmpty(nick))
                    nick = $"Player {player.RawEncoded}";

                var networkObj = await runner.SpawnAsync(
                    carPrefab,
                    position,
                    Quaternion.identity,
                    player,
                    InitializeCar
                );
 
                var car = networkObj.GetComponent<NetworkCar>();
                car.SetName(nick);
                runner.SetPlayerObject(player, networkObj);
            }
        }

        private void InitializeCar(NetworkRunner runner, NetworkObject networkObj)
        {
            var car = networkObj.GetComponent<NetworkCar>();
            car.Initialize(carConfig);
        }
    }
}