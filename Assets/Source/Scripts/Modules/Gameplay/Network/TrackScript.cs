using System.Collections.Generic;
using Fusion;
using TojGamesTask.Modules.Gameplay.Types;
using UnityEngine;

namespace TojGamesTask.Modules.Gameplay.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class TrackScript : NetworkBehaviour
    {
        [SerializeField]
        private List<SpawnPoint> spawnPoints = new();
        [SerializeField] private FinishTrigger finish;
        [Networked] public float RaceStartTime { get; set; }

        public IReadOnlyList<SpawnPoint> SpawnPoints => spawnPoints;
        public FinishTrigger FinishTrigger => finish;

        public void SetStartTime(float t)
        {
            if (Object.HasStateAuthority)
                RaceStartTime = t;
        }
    }
}