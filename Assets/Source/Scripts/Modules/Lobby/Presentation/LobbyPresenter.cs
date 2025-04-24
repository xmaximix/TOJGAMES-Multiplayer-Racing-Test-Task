// LobbyPresenter.cs

using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using R3;
using TojGamesTask.Common.Extensions;
using TojGamesTask.Common.Networking;
using TojGamesTask.Common.SceneManagement;
using TojGamesTask.Modules.Lobby.Domain;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using ILogger = TojGamesTask.Common.Logging.ILogger;

namespace TojGamesTask.Modules.Lobby.Presentation
{
    public sealed class LobbyPresenter : IInitializable, IDisposable
    {
        readonly LobbyView _view;
        readonly ILobbySystem _lobby;
        readonly INetworkService _net;
        readonly ISceneService _scenes;
        readonly ILogger _log;
        readonly CompositeDisposable _d = new CompositeDisposable();

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
            _view.NamePanel.SetActive(true);
            _view.LobbyPanel.SetActive(false);

            _view.JoinButton.OnClickAsObservable()
                .Subscribe(_ => JoinFlow().Forget())
                .AddTo(_d);

            _view.StartButton.OnClickAsObservable()
                .Subscribe(_ => _lobby.StartCommand.Execute(Unit.Default))
                .AddTo(_d);

            _view.LeaveButton.OnClickAsObservable()
                .Subscribe(_ => _lobby.LeaveCommand.Execute(Unit.Default))
                .AddTo(_d);

            _lobby.IsHost
                .Subscribe(h => {
                    _view.StartButton.gameObject.SetActive(h);
                    _view.LeaveButton.gameObject.SetActive(true);
                })
                .AddTo(_d);

            _lobby.Players.ObserveAdd()
                .Select(_ => Unit.Default)
                .Merge(_lobby.Players.ObserveRemove().Select(_ => Unit.Default))
                .Subscribe(_ => {
                    _view.PlayerCountText.text = $"Players Count: {_lobby.Players.Count}";
                    RefreshPlayerList();
                })
                .AddTo(_d);

            _lobby.StartCommand
                .Subscribe(_ => _scenes.LoadSceneAsync("Race").Forget())
                .AddTo(_d);

            _lobby.LeaveCommand
                .Subscribe(_ => {
                    _net.Shutdown();
                    _view.LobbyPanel.SetActive(false);
                    _view.NamePanel.SetActive(true);
                })
                .AddTo(_d);

            _net.SessionEnded
                .Subscribe(_ => {
                    _view.LobbyPanel.SetActive(false);
                    _view.NamePanel.SetActive(true);
                    _lobby.LeaveCommand.Execute(Unit.Default);
                })
                .AddTo(_d);
        }

        async UniTaskVoid JoinFlow()
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

            _view.LobbyPanel.SetActive(true);
            _view.NamePanel.SetActive(false);
            _view.LobbyCodeText.text = $"Lobby Code: {_net.Runner.SessionInfo.Name}";

            await UniTask.WaitUntil(() => _net.Runner.TryGetPlayerObject(_net.LocalPlayer, out _));
            var avatar = _net.Runner.GetPlayerObject(_net.LocalPlayer).GetComponent<PlayerAvatar>();
            avatar.RPC_SetName(name);

            _log.Log($"Joined lobby '{code}' as '{name}'.");
        }

        void RefreshPlayerList()
        {
            var names = _lobby.Players.Select(p => p.Name).ToArray();
            _view.PlayerListText.text = string.Join("\n", names);
        }

        public void Dispose() => _d.Dispose();
    }
}