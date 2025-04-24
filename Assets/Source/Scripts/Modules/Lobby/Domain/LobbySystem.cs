using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using ObservableCollections;
using R3;
using TojGamesTask.Common.Networking;
using UnityEngine;

namespace TojGamesTask.Modules.Lobby.Domain
{
    public sealed class LobbySystem : ILobbySystem, IDisposable
    {
        private readonly INetworkService _net;
        private readonly ObservableList<PlayerInfo> _players = new();
        private readonly HashSet<PlayerRef> _pending = new();
        private readonly ReactiveProperty<bool> _isHostInt = new(false);
        private readonly ReadOnlyReactiveProperty<bool> _isHost;
        private readonly ReactiveCommand _startCmd = new();
        private readonly ReactiveCommand _leaveCmd = new();
        private readonly CompositeDisposable _d = new();

        public IReadOnlyObservableList<PlayerInfo> Players => _players;
        public ReadOnlyReactiveProperty<bool> IsHost => _isHost;
        public ReactiveCommand StartCommand => _startCmd;
        public ReactiveCommand LeaveCommand => _leaveCmd;

        public LobbySystem(INetworkService network)
        {
            _net = network;
            _isHost = _isHostInt.ToReadOnlyReactiveProperty().AddTo(_d);
            _leaveCmd.Subscribe(_ => _players.Clear()).AddTo(_d);

            _net.PlayerJoined
                .Subscribe(tuple => OnJoined(tuple.Item1, tuple.Item2))
                .AddTo(_d);

            _net.PlayerLeft
                .Subscribe(tuple => OnLeft(tuple.Item1, tuple.Item2))
                .AddTo(_d);
        }

        private void OnJoined(NetworkRunner _, PlayerRef pr)
        {
            if (pr == _net.LocalPlayer)
                _isHostInt.Value = _net.IsHost;
            
            if (_players.Any(x => x.Id == pr) || !_pending.Add(pr))
                return;

            GetAvatar(pr).ContinueWith(() =>
                _pending.Remove(pr)).Forget();
        }

        private async UniTask GetAvatar(PlayerRef pr)
        {
            var avatar = await _net.GetAvatarAsync(pr);
            if (avatar != null)
                SubscribeToNameChange(pr, avatar);
        }

        private void SubscribeToNameChange(PlayerRef pr, PlayerAvatar avatar)
        {
            avatar.NameChanged
                .Where(eventData => eventData.Item1 == pr)
                .Select(eventData => eventData.Item2)
                .Take(1)
                .Subscribe(newName => TryAddPlayer(pr, newName, _players.All(p => p.Id != pr)))
                .AddTo(_d);
        }

        private void TryAddPlayer(PlayerRef pr, string name, bool condition)
        {
            if (!condition)
                return;

            _players.Add(new PlayerInfo(pr, name));
        }

        private void OnLeft(NetworkRunner _, PlayerRef pr)
        {
            var e = _players.FirstOrDefault(x => x.Id == pr);
            _players.Remove(e);
        }

        public void Dispose()
        {
            _d.Dispose();
            _isHostInt.Dispose();
        }
    }
}