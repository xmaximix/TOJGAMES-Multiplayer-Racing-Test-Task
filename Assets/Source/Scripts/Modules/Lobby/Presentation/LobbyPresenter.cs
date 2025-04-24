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
        private readonly LobbyView _view;
        private readonly ILobbySystem _lobby;
        private readonly INetworkService _net;
        private readonly ISceneService _scenes;
        private readonly ILogger _log;
        private readonly CompositeDisposable _d = new();

        [Inject]
        public LobbyPresenter(
            LobbyView view,
            ILobbySystem lobbySystem,
            INetworkService net,
            ISceneService scenes,
            ILogger log)
        {
            _view = view;
            _lobby = lobbySystem;
            _net = net;
            _scenes = scenes;
            _log = log;
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
            _view.NamePanel.SetActive(true);
            _view.LobbyPanel.SetActive(false);
        }

        private void BindViewActions()
        {
            _view.JoinButton.OnClickAsObservable()
                .Subscribe(_ => JoinFlow().Forget())
                .AddTo(_d);

            _view.StartButton.OnClickAsObservable()
                .Subscribe(_ => _lobby.StartCommand.Execute(Unit.Default))
                .AddTo(_d);

            _view.LeaveButton.OnClickAsObservable()
                .Subscribe(_ => _lobby.LeaveCommand.Execute(Unit.Default))
                .AddTo(_d);
        }

        private void BindLobbyEvents()
        {
            _lobby.IsHost
                .Subscribe(isHost =>
                {
                    _view.StartButton.gameObject.SetActive(isHost);
                    _view.LeaveButton.gameObject.SetActive(true);
                })
                .AddTo(_d);

            var playerCountChanged = _lobby.Players.ObserveAdd()
                .Select(_ => Unit.Default)
                .Merge(_lobby.Players.ObserveRemove().Select(_ => Unit.Default));

            playerCountChanged
                .Subscribe(_ =>
                {
                    _view.PlayerCountText.text = $"Players Count: {_lobby.Players.Count}";
                    RefreshPlayerList();
                })
                .AddTo(_d);

            _lobby.StartCommand
                .Subscribe(_ =>
                {
                    if (!_net.IsHost)
                        return;

                    int raceIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Source/Scenes/Race.unity");
                    var raceRef   = SceneRef.FromIndex(raceIndex);

                    _net.Runner.LoadScene(
                        raceRef   
                    );
                })
                .AddTo(_d);

            _lobby.LeaveCommand
                .Subscribe(_ => HandleLeave())
                .AddTo(_d);
        }

        private void BindNetworkEvents()
        {
            _net.SessionEnded
                .Subscribe(_ => HandleSessionEnd())
                .AddTo(_d);
        }

        private async UniTaskVoid JoinFlow()
        {
            var name = _view.NameInput.text.Trim();
            var code = _view.LobbyCodeInput.text.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
            {
                _log.LogWarning("Name or lobby code empty.");
                return;
            }

            if (!await _net.StartGameAsync(code, name))
            {
                _log.LogWarning($"Failed to join lobby '{code}'.");
                return;
            }

            EnableLobbyPanel();
            await AssignLocalPlayerName(name);
            _log.Log($"Joined lobby '{code}' as '{name}'.");
        }

        private void EnableLobbyPanel()
        {
            _view.LobbyPanel.SetActive(true);
            _view.NamePanel.SetActive(false);
            _view.LobbyCodeText.text = $"Lobby Code: {_net.Runner.SessionInfo.Name}";
        }

        private async UniTask AssignLocalPlayerName(string name)
        {
            await UniTask.WaitUntil(() => _net.Runner.TryGetPlayerObject(_net.LocalPlayer, out _));
            var avatar = _net.Runner.GetPlayerObject(_net.LocalPlayer).GetComponent<PlayerAvatar>();
            avatar.RPC_SetName(name);
        }

        private void HandleLeave()
        {
            _net.Shutdown();
            SetupPanels();
        }

        private void HandleSessionEnd()
        {
            SetupPanels();
            _lobby.LeaveCommand.Execute(Unit.Default);
        }

        private void RefreshPlayerList()
        {
            _view.PlayerListText.text = string.Join("\n", _lobby.Players.Select(p => p.Name));
        }

        public void Dispose()
        {
            _d.Dispose();
        }
    }
}