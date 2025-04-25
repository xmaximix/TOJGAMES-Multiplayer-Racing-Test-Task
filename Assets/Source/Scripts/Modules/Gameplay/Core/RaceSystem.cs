using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using R3;
using VContainer;
using TojGamesTask.Common.Networking;
using TojGamesTask.Modules.Gameplay.Network;

namespace TojGamesTask.Modules.Gameplay.Core
{
    public sealed class RaceSystem : IDisposable
    {
        private readonly TrackScript track;
        private readonly INetworkService networkService;

        private readonly List<(PlayerRef player, float time)> finishedPlayers = new();
        private readonly CompositeDisposable d = new();

        private readonly Subject<(PlayerRef player, int place, float time)> playerFinished = new();
        private readonly Subject<List<(PlayerRef player, int place, float time)>> raceFinished = new();

        private int expectedCount;

        public Subject<(PlayerRef player, int place, float time)> PlayerFinished => playerFinished;

        public RaceSystem(TrackScript track, INetworkService networkService)
        {
            this.track = track;
            this.networkService = networkService;
        }

        public void Init(IEnumerable<PlayerRef> players)
        {
            var playerList = players as PlayerRef[] ?? players.ToArray();
            expectedCount = playerList.Length;

            track.FinishTrigger.OnCrossed
                .Where(player => finishedPlayers.All(fp => fp.player != player))
                .Subscribe(OnPlayerFinished)
                .AddTo(d);
        }

        private void OnPlayerFinished(PlayerRef player)
        {
            float startTime = track.RaceStartTime;
            float elapsed = networkService.Runner.SimulationTime - startTime;
            float roundedTime  = Mathf.Round(elapsed * 4f) / 4f;
            
            finishedPlayers.Add((player, roundedTime));

            int place = finishedPlayers.Count;
            playerFinished.OnNext((player, place, roundedTime));

            if (place == expectedCount)
            {
                var results = finishedPlayers
                    .OrderBy(fp => fp.time)
                    .Select((fp, index) => (fp.player, place: index + 1, fp.time))
                    .ToList();

                raceFinished.OnNext(results);
            }
        }

        public void Dispose() => d.Dispose();
    }
}