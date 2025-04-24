using Fusion;
using R3;

namespace TojGamesTask.Common.Networking
{
    public class PlayerAvatar : NetworkBehaviour
    {
        [Networked]
        public string DisplayName { get; private set; }

        private readonly ReplaySubject<(PlayerRef player, string name)> _nameChangedSubject = new(1);
        public ReplaySubject<(PlayerRef player, string name)> NameChanged => _nameChangedSubject;

        public bool HasName => !string.IsNullOrEmpty(DisplayName);

        private string _lastName;

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetName(string value)
        {
            DisplayName = value;
        }

        public override void Spawned()
        {
            if (HasName)
                PublishName(Object.InputAuthority, DisplayName);
        }

        public override void Render()
        {
            if (HasName && DisplayName != _lastName)
                PublishName(Object.InputAuthority, DisplayName);
        }

        private void PublishName(PlayerRef player, string name)
        {
            _lastName = name;
            _nameChangedSubject.OnNext((player, name));
        }
    }
}