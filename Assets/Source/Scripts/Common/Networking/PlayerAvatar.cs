using Fusion;
using R3;

namespace TojGamesTask.Common.Networking
{
    public class PlayerAvatar : NetworkBehaviour
    {
        [Networked]
        public string DisplayName { get; private set; }

        private readonly ReplaySubject<(PlayerRef player, string name)> nameChangedSubject = new(1);
        public ReplaySubject<(PlayerRef player, string name)> NameChanged => nameChangedSubject;

        public bool HasName => !string.IsNullOrEmpty(DisplayName);

        private string lastName;
        private INetworkService networkService;

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetName(string value)
        {
            DisplayName = value;
        }
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SendName(string nick)
        {
            networkService.RegisterNickname(Object.InputAuthority, nick); 
        }

        public override void Spawned()
        {
            if (HasName)
                PublishName(Object.InputAuthority, DisplayName);
        }

        public void SetNetworkService(INetworkService net)
        {
            networkService = net;
        }

        public override void Render()
        {
            if (HasName && DisplayName != lastName)
                PublishName(Object.InputAuthority, DisplayName);
        }

        private void PublishName(PlayerRef player, string name)
        {
            lastName = name;
            nameChangedSubject.OnNext((player, name));
        }
    }
}