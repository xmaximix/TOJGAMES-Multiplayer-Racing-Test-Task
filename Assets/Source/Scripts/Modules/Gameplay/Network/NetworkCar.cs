using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;
using TojGamesTask.Common.Input;
using TojGamesTask.Modules.Gameplay.Configs;
using TojGamesTask.Modules.Gameplay.Core;

namespace TojGamesTask.Modules.Gameplay.Network
{
    [RequireComponent(
        typeof(NetworkObject),
        typeof(NetworkRigidbody3D),
        typeof(Rigidbody)
    )]
    public sealed class NetworkCar : NetworkBehaviour
    {
        [SerializeField] private CameraFollow cameraFollowPrefab;
        [Networked] public string DisplayName { get; private set; }
        private Rigidbody rb;
        private CarMovement movement;
        private bool isStateAuthority;
        private bool isLocalPlayer;

        public void Initialize(CarConfig config)
        {
            rb = GetComponent<Rigidbody>();
            movement = new CarMovement(rb, config.MaxSpeed, config.Acceleration, config.TurnSpeed);
        }

        public void SetName(string name)
        {
            DisplayName = name;
        }

        public override void Spawned()
        {
            base.Spawned();
            rb = GetComponent<Rigidbody>();
            isStateAuthority = HasStateAuthority;
            isLocalPlayer = HasInputAuthority;

            Runner.SetIsSimulated(Object, isStateAuthority);
            rb.isKinematic = !isStateAuthority;

            if (isLocalPlayer)
            {
                var cam = Instantiate(cameraFollowPrefab);
                cam.SetTarget(transform);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!isStateAuthority || movement == null)
                return;

            if (Runner.TryGetInputForPlayer(Object.InputAuthority, out PlayerInputData input))
            {
                movement.Move(input.throttle, input.steer);
            }
        }
    }
}