using Fusion;
using R3;
using UnityEngine;

public class PlayerAvatar : NetworkBehaviour
{
    [Networked] public string DisplayName { get; private set; }

    readonly ReplaySubject<(PlayerRef, string)> _nameChanged =
        new ReplaySubject<(PlayerRef, string)>(1);

    public ReplaySubject<(PlayerRef, string)> NameChanged => _nameChanged;
    public bool HasName => !string.IsNullOrEmpty(DisplayName);

    string _cached;       

    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
    public void RPC_SetName(string value) => DisplayName = value;

    public override void Spawned()
    {
        if (HasName)
        {
            _cached = DisplayName;
            _nameChanged.OnNext((Object.InputAuthority, DisplayName));
        }
    }

    public override void Render()
    {
        if (DisplayName != _cached && HasName)
        {
            _cached = DisplayName;
            _nameChanged.OnNext((Object.InputAuthority, DisplayName));
        }
    }
}