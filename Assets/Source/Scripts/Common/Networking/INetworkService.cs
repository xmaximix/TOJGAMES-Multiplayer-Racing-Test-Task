using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;

namespace TojGamesTask.Common.Networking
{
    public interface INetworkService
    {
        NetworkRunner Runner { get; }
        Subject<Unit> SessionEnded { get; }

        Subject<(NetworkRunner runner, PlayerRef player)> PlayerJoined { get; }
        Subject<(NetworkRunner runner, PlayerRef player)> PlayerLeft { get; }
        IReadOnlyDictionary<PlayerRef,string> Nicknames { get; }
        bool IsHost { get; }
        PlayerRef LocalPlayer { get; }
        UniTask<PlayerAvatar> GetAvatarAsync(PlayerRef player);
        UniTask<bool> StartGameAsync(string sessionName, string playerName);
        void Shutdown();
        void RegisterNickname(PlayerRef pr, string nick);
    }
}