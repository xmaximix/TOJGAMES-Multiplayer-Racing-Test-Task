using System;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;

namespace TojGamesTask.Common.Networking
{
    public interface INetworkService
    {
        NetworkRunner Runner { get; }
        Subject<Unit> SessionEnded { get; }
        event Action<NetworkRunner, PlayerRef> PlayerJoined;
        event Action<NetworkRunner, PlayerRef> PlayerLeft;
        bool IsHost { get; }
        PlayerRef LocalPlayer { get; }
        UniTask<bool> StartGameAsync(string sessionName, string playerName);
        void Shutdown();
    }
}