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
        private readonly INetworkService net;
        private readonly ObservableList<PlayerInfo> players = new();
        private readonly HashSet<PlayerRef> pending = new();
        private readonly ReactiveProperty<bool> isHostInt = new(false);
        private readonly ReadOnlyReactiveProperty<bool> isHost;
        private readonly ReactiveCommand startCmd = new();
        private readonly ReactiveCommand leaveCmd = new();
        private readonly CompositeDisposable d = new();

        public IReadOnlyObservableList<PlayerInfo> Players => players;
        public ReadOnlyReactiveProperty<bool> IsHost => isHost;
        public ReactiveCommand StartCommand => startCmd;
        public ReactiveCommand LeaveCommand => leaveCmd;

        public LobbySystem(INetworkService network)
        {
            net = network;
            isHost = isHostInt.ToReadOnlyReactiveProperty().AddTo(d);
            leaveCmd.Subscribe(_ => players.Clear()).AddTo(d);

            net.PlayerJoined
                .Subscribe(tuple => OnJoined(tuple.Item1, tuple.Item2))
                .AddTo(d);

            net.PlayerLeft
                .Subscribe(tuple => OnLeft(tuple.Item1, tuple.Item2))
                .AddTo(d);
        }

        private void OnJoined(NetworkRunner _, PlayerRef pr)
        {
            if (pr == net.LocalPlayer)
                isHostInt.Value = net.IsHost;

            if (players.Any(x => x.Id == pr) || !pending.Add(pr))
                return;

            GetAvatar(pr).ContinueWith(() =>
                pending.Remove(pr)).Forget();
        }

        private async UniTask GetAvatar(PlayerRef pr)
        {
            var avatar = await net.GetAvatarAsync(pr);
            if (avatar != null)
                SubscribeToNameChange(pr, avatar);
        }

        private void SubscribeToNameChange(PlayerRef pr, PlayerAvatar avatar)
        {
            avatar.NameChanged
                .Where(eventData => eventData.Item1 == pr)
                .Select(eventData => eventData.Item2)
                .Take(1)
                .Subscribe(newName => TryAddPlayer(pr, newName, players.All(p => p.Id != pr)))
                .AddTo(d);
        }

        private void TryAddPlayer(PlayerRef pr, string name, bool condition)
        {
            if (!condition)
                return;

            players.Add(new PlayerInfo(pr, name));
        }

        private void OnLeft(NetworkRunner _, PlayerRef pr)
        {
            var e = players.FirstOrDefault(x => x.Id == pr);
            players.Remove(e);
        }

        public void Dispose()
        {
            d.Dispose();
            isHostInt.Dispose();
        }
    }
}