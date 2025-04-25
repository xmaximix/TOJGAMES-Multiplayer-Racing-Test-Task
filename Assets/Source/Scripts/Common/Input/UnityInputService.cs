using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace TojGamesTask.Common.Input
{
    public sealed class UnityInputService : IInputService, IDisposable
    {
        private readonly InputAction steer;
        private readonly InputAction throttle;
        private readonly InputActionAsset asset;

        public UnityInputService(InputActionAsset inputActions)
        {
            asset = inputActions;
            var map = asset.FindActionMap("Gameplay", throwIfNotFound: true);
            steer = map.FindAction("Steer", throwIfNotFound: true);
            throttle = map.FindAction("Throttle", throwIfNotFound: true);
            asset.Enable();
        }
        
        public float Steer => steer.ReadValue<float>();
        public float Throttle => throttle.ReadValue<float>();
        public void Dispose() => asset.Disable();
    }
}