// LobbySystem.cs
using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using ObservableCollections;
using R3;
using TojGamesTask.Common.Networking;

namespace TojGamesTask.Modules.Lobby.Domain
{
    public sealed class LobbySystem : ILobbySystem, IDisposable
    {
        readonly INetworkService _net;
        readonly ObservableList<PlayerInfo> _players = new();
        readonly HashSet<PlayerRef> _pending = new();
        readonly ReactiveProperty<bool> _isHostInt = new(false);
        readonly ReadOnlyReactiveProperty<bool> _isHost;
        readonly ReactiveCommand _startCmd = new();
        readonly ReactiveCommand _leaveCmd = new();
        readonly CompositeDisposable _cleanup = new();

        public LobbySystem(INetworkService network)
        {
            _net = network;
            _isHost = _isHostInt.ToReadOnlyReactiveProperty().AddTo(_cleanup);
            _net.PlayerJoined += OnJoined;
            _net.PlayerLeft += OnLeft;
            _leaveCmd
                .Subscribe(_ => _players.Clear())
                .AddTo(_cleanup);
        }

        public IReadOnlyObservableList<PlayerInfo> Players => _players;
        public ReadOnlyReactiveProperty<bool> IsHost => _isHost;
        public ReactiveCommand StartCommand => _startCmd;
        public ReactiveCommand LeaveCommand => _leaveCmd;

        void OnJoined(NetworkRunner runner, PlayerRef pr)
        {
            if (pr == _net.LocalPlayer)
                _isHostInt.Value = _net.IsHost;
            if (_players.Any(x => x.Id == pr) || _pending.Contains(pr))
                return;
            _pending.Add(pr);
            Subscribe(runner, pr)
                .ContinueWith(() => _pending.Remove(pr))
                .Forget();
        }

        async UniTask Subscribe(NetworkRunner runner, PlayerRef pr)
        {
            await UniTask.WaitUntil(() => runner != null && runner.TryGetPlayerObject(pr, out _));
            var go = runner.GetPlayerObject(pr);
            if (go == null) return;
            var avatar = go.GetComponent<PlayerAvatar>();
            if (avatar.HasName)
                _players.Add(new PlayerInfo(pr, avatar.DisplayName));
            avatar.NameChanged
                .Where(t => t.Item1 == pr)
                .Take(1)
                .Subscribe(t =>
                {
                    if (_players.All(p => p.Id != pr))
                        _players.Add(new PlayerInfo(pr, t.Item2));
                })
                .AddTo(_cleanup);
        }

        void OnLeft(NetworkRunner runner, PlayerRef pr)
        {
            var e = _players.FirstOrDefault(x => x.Id == pr);
            _players.Remove(e);
        }

        public void Dispose()
        {
            _net.PlayerJoined -= OnJoined;
            _net.PlayerLeft -= OnLeft;
            _cleanup.Dispose();
            _isHostInt.Dispose();
        }
    }
}