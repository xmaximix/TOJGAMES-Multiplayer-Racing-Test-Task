using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace TojGamesTask.Common.Input
{
    public sealed class UnityInputService : IInputService, IDisposable
    {
        private readonly InputAction _steer;
        private readonly InputAction _throttle;
        private readonly InputActionAsset _asset;

        public UnityInputService(InputActionAsset inputActions)
        {
            _asset = inputActions;
            var map = _asset.FindActionMap("Gameplay", throwIfNotFound: true);
            _steer = map.FindAction("Steer", throwIfNotFound: true);
            _throttle = map.FindAction("Throttle", throwIfNotFound: true);
            _asset.Enable();
        }
        
        public float Steer => _steer.ReadValue<float>();
        public float Throttle => _throttle.ReadValue<float>();
        public void Dispose() => _asset.Disable();
    }
}