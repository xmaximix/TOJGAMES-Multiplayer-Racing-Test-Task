using System;
using ObservableCollections;
using R3;
using TojGamesTask.Common.Networking;

namespace TojGamesTask.Modules.Lobby.Domain
{
    public interface ILobbySystem
    {
        IReadOnlyObservableList<PlayerInfo> Players { get; }
        ReadOnlyReactiveProperty<bool> IsHost { get; }
        ReactiveCommand StartCommand { get; }
        ReactiveCommand LeaveCommand { get; }
    }
}