using System;
using Fusion;
using R3;
using UnityEngine;

namespace TojGamesTask.Modules.Gameplay.Network
{
    [RequireComponent(typeof(Collider))]
    public sealed class FinishTrigger : NetworkBehaviour, IDisposable
    {
        private readonly Subject<PlayerRef> crossed = new();

        public Subject<PlayerRef> OnCrossed => crossed;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<NetworkObject>(out var nob))
            {
                crossed.OnNext(nob.InputAuthority);
            }
        }

        public void Dispose()
        {
            crossed.OnCompleted();
        }
    }
}