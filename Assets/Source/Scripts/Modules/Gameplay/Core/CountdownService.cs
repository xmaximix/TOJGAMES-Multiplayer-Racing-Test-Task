using System;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;
using TojGamesTask.Common.Networking;
using TojGamesTask.Modules.Gameplay.Configs;
using TojGamesTask.Modules.Gameplay.Network;
using VContainer;

namespace TojGamesTask.Modules.Gameplay.Core
{
    public sealed class CountdownService : IDisposable
    {
        private readonly TrackScript track;
        private readonly INetworkService network;
        private readonly CountdownConfig config;

        private readonly Subject<int> ticks = new();
        private readonly Subject<Unit> finished = new();

        public Subject<int> Ticks => ticks;
        public Subject<Unit> Finished => finished;

        [Inject]
        public CountdownService(
            TrackScript track,
            INetworkService network,
            CountdownConfig config)
        {
            this.track = track;
            this.network = network;
            this.config = config;
        }

        public async UniTask Run()
        {
            var runner = network.Runner;

            if (runner.IsServer)
                track.SetStartTime(runner.SimulationTime);

            await WaitForNetworkRaceStart();

            var startTime = track.RaceStartTime;
            await NetworkRunnerExtensions.WaitUntilTimeReached(runner, startTime);

            for (var count = config.CountdownSeconds; count >= 1; count--)
            {
                ticks.OnNext(count);
                await NetworkRunnerExtensions.WaitUntilTimeReached(runner, startTime + (config.CountdownSeconds - count + 1));
            }

            finished.OnNext(Unit.Default);
        }

        private UniTask WaitForNetworkRaceStart()
        {
            return UniTask.WaitUntil(() =>
                TryGetRaceStartTime(out _)
            );
        }

        private bool TryGetRaceStartTime(out float time)
        {
            try
            {
                time = track.RaceStartTime;
                return true;
            }
            catch (InvalidOperationException)
            {
                time = default;
                return false;
            }
        }

        private static class NetworkRunnerExtensions
        {
            public static UniTask WaitUntilTimeReached(NetworkRunner runner, float targetTime)
            {
                return UniTask.WaitUntil(() => runner.SimulationTime >= targetTime);
            }
        }

        public void Dispose()
        {
            ticks.OnCompleted();
            finished.OnCompleted();
        }
    }
}