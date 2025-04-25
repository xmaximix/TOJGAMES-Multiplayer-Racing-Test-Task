using System;
using System.Collections;
using System.Linq;
using Fusion;
using UnityEngine;
using TojGamesTask.Common.Networking;
using TojGamesTask.Common.Utils;
using TojGamesTask.Modules.Gameplay.Network;
using TojGamesTask.Modules.Gameplay.Presentation.Views;
using VContainer.Unity;

namespace TojGamesTask.Modules.Gameplay.Presentation.Presenters
{
    public sealed class HUDPresenter : IStartable, IDisposable
    {
        private const float UpdateInterval = 0.16f;

        private readonly HUDView view;
        private readonly INetworkService networkService;
        private readonly TrackScript track;

        private Vector3 startPoint;
        private Coroutine rankCoroutine;

        public HUDPresenter(
            HUDView view,
            INetworkService networkService,
            TrackScript track)
        {
            this.view = view;
            this.networkService = networkService;
            this.track = track;
        }

        public void Start()
        {
            InitializeTrackMetrics();
            rankCoroutine = StaticCoroutine.StartCoroutine(RankLoop());
        }

        private void InitializeTrackMetrics()
        {
            startPoint = track.SpawnPoints[0].transform.position;
        }

        private IEnumerator RankLoop()
        {
            var runner = networkService.Runner;
            while (true)
            {
                DisplayRank(runner);
                yield return new WaitForSeconds(UpdateInterval);
            }
        }

        private void DisplayRank(NetworkRunner runner)
        {
            if (runner == null) return;

            var finishPos = track.FinishTrigger.transform.position;

            var standings = runner.ActivePlayers
                .Select(pr =>
                {
                    if (runner.TryGetPlayerObject(pr, out var obj) && obj != null)
                    {
                        var d = Vector3.Distance(obj.transform.position, finishPos);
                        return (player: pr, dist: d);
                    }
                    return (player: pr, dist: float.MaxValue);
                })
                .OrderBy(entry => entry.dist)
                .ToList();

            var rank = standings.FindIndex(entry => entry.player == runner.LocalPlayer) + 1;
            var total = runner.ActivePlayers.Count();

            view.PlaceText.SetText($"Position: {rank}/{total}");
        }

        public void Dispose()
        {
            if (rankCoroutine != null)
                StaticCoroutine.StopCoroutine(rankCoroutine);
        }
    }
}