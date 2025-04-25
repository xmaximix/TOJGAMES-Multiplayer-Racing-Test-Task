using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using ObservableCollections;
using R3;
using TojGamesTask.Common.Extensions;
using TojGamesTask.Common.Networking;
using TojGamesTask.Common.SceneManagement;
using TojGamesTask.Modules.Lobby.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using ILogger = TojGamesTask.Common.Logging.ILogger;

namespace TojGamesTask.Modules.Lobby.Presentation
{
    public sealed class LobbyPresenter : IInitializable, IDisposable
    {
        private readonly LobbyView view;
        private readonly ILobbySystem lobby;
        private readonly INetworkService net;
        private readonly ISceneService scenes;
        private readonly ILogger log;
        private readonly CompositeDisposable d = new();
        private PlayerAvatar avatar;

        [Inject]
        public LobbyPresenter(
            LobbyView view,
            ILobbySystem lobbySystem,
            INetworkService net,
            ISceneService scenes,
            ILogger log)
        {
            this.view = view;
            lobby = lobbySystem;
            this.net = net;
            this.scenes = scenes;
            this.log = log;
        }

        public void Initialize()
        {
            SetupPanels();
            BindViewActions();
            BindLobbyEvents();
            BindNetworkEvents();
        }

        private void SetupPanels()
        {
            view.NamePanel.SetActive(true);
            view.LobbyPanel.SetActive(false);
        }

        private void BindViewActions()
        {
            view.JoinButton.OnClickAsObservable()
                .Subscribe(_ => JoinFlow().Forget())
                .AddTo(d);

            view.StartButton.OnClickAsObservable()
                .Subscribe(_ => lobby.StartCommand.Execute(Unit.Default))
                .AddTo(d);

            view.LeaveButton.OnClickAsObservable()
                .Subscribe(_ => lobby.LeaveCommand.Execute(Unit.Default))
                .AddTo(d);
        }

        private void BindLobbyEvents()
        {
            lobby.IsHost
                .Subscribe(isHost =>
                {
                    view.StartButton.gameObject.SetActive(isHost);
                    view.LeaveButton.gameObject.SetActive(true);
                })
                .AddTo(d);

            var playerCountChanged = lobby.Players.ObserveAdd()
                .Select(_ => Unit.Default)
                .Merge(lobby.Players.ObserveRemove().Select(_ => Unit.Default));

            playerCountChanged
                .Subscribe(_ =>
                {
                    view.PlayerCountText.text = $"Players Count: {lobby.Players.Count}";
                    RefreshPlayerList();
                })
                .AddTo(d);

            lobby.StartCommand
                .Subscribe(_ =>
                {
                    if (!net.IsHost)
                        return;

                    var raceIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Source/Scenes/Race.unity");
                    var raceRef = SceneRef.FromIndex(raceIndex);

                    net.Runner.LoadScene(
                        raceRef
                    );
                })
                .AddTo(d);

            lobby.LeaveCommand
                .Subscribe(_ => HandleLeave())
                .AddTo(d);
        }

        private void BindNetworkEvents()
        {
            net.SessionEnded
                .Subscribe(_ => HandleSessionEnd())
                .AddTo(d);
        }

        private async UniTaskVoid JoinFlow()
        {
            var name = view.NameInput.text.Trim();
            var code = view.LobbyCodeInput.text.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
            {
                log.LogWarning("Name or lobby code empty.");
                return;
            }

            if (!await net.StartGameAsync(code, name))
            {
                log.LogWarning($"Failed to join lobby '{code}'.");
                return;
            }

            EnableLobbyPanel();
            await AssignLocalPlayerName(name);
            await UniTask.WaitUntil(() => net.Runner.LocalPlayer != default);
            net.RegisterNickname(net.LocalPlayer, name);

            if (!net.IsHost && avatar != null)
            {
                avatar.RPC_SendName(name);
            }
            
            log.Log($"Joined lobby '{code}' as '{name}'.");
        }

        private void EnableLobbyPanel()
        {
            view.LobbyPanel.SetActive(true);
            view.NamePanel.SetActive(false);
            view.LobbyCodeText.text = $"Lobby Code: {net.Runner.SessionInfo.Name}";
        }

        private async UniTask AssignLocalPlayerName(string name)
        {
            await UniTask.WaitUntil(() => net.Runner.TryGetPlayerObject(net.LocalPlayer, out _));
            avatar = net.Runner.GetPlayerObject(net.LocalPlayer).GetComponent<PlayerAvatar>();
            avatar.RPC_SetName(name);
        }

        private void HandleLeave()
        {
            net.Shutdown();
            SetupPanels();
        }

        private void HandleSessionEnd()
        {
            SetupPanels();
            lobby.LeaveCommand.Execute(Unit.Default);
        }

        private void RefreshPlayerList()
        {
            view.PlayerListText.text = string.Join("\n", lobby.Players.Select(p => p.Name));
        }

        public void Dispose()
        {
            d.Dispose();
        }
    }
}