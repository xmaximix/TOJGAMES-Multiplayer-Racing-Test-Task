using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using R3;
using VContainer;
using TojGamesTask.Common.Networking;
using TojGamesTask.Modules.Gameplay.Core;
using TojGamesTask.Modules.Gameplay.Network;
using TojGamesTask.Modules.Gameplay.Presentation.Views;
using VContainer.Unity;

namespace TojGamesTask.Modules.Gameplay.Presenters
{
    public sealed class FinishPresenter : IStartable, IDisposable
    {
        private readonly FinishView view;
        private readonly RaceSystem raceSystem;
        private readonly INetworkService networkService;
        private readonly CompositeDisposable disposables = new();

        private readonly List<(PlayerRef player, int place, float time)> results = new();

        private readonly Dictionary<PlayerRef, string> nameMap = new();

        public FinishPresenter(
            FinishView view,
            RaceSystem raceSystem,
            INetworkService networkService)
        {
            this.view = view;
            this.raceSystem = raceSystem;
            this.networkService = networkService;
        }

        public void Start()
        {
            view.Panel.SetActive(false);

            raceSystem.PlayerFinished
                .Subscribe(OnPlayerFinished)
                .AddTo(disposables);
        }

        private void OnPlayerFinished((PlayerRef player, int place, float time) data)
        {
            results.Add(data);

            if (data.player == networkService.LocalPlayer)
                view.Panel.SetActive(true);

            CacheOrAwaitName(data.player);
            RefreshLeaderboard();
        }

        private void CacheOrAwaitName(PlayerRef player)
        {
            if (nameMap.ContainsKey(player))
                return;

            if (networkService.Runner.TryGetPlayerObject(player, out var obj)
                && obj.GetComponent<PlayerAvatar>() is { } avatar)
            {
                var displayName = avatar.DisplayName;
                if (!string.IsNullOrEmpty(displayName))
                {
                    nameMap[player] = displayName;
                }
                else
                {
                    avatar.NameChanged
                        .Where(change => change.Item1 == player)
                        .Take(1)
                        .Subscribe(change =>
                        {
                            nameMap[player] = change.Item2;
                            RefreshLeaderboard();
                        })
                        .AddTo(disposables);
                }
            }
        }

        private void RefreshLeaderboard()
        {
            var lines = results
                .OrderBy(r => r.place)
                .Select(r => $"{r.place}. {GetName(r.player)}  {r.time:F1}s");

            view.ResultsText.text = string.Join("\n", lines);
        }

        private string GetName(PlayerRef player)
        {
            if (networkService.Runner.TryGetPlayerObject(player, out var obj)
                && obj.GetComponent<NetworkCar>() is { } car
                && !string.IsNullOrEmpty(car.DisplayName))
            {
                return car.DisplayName;
            }

            return $"Player {player.RawEncoded}";
        }

        public void Dispose() => disposables.Dispose();
    }
}