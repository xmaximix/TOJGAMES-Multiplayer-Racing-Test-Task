// FusionNetworkService.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using R3;
using TojGamesTask.Common.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using Object = UnityEngine.Object;

namespace TojGamesTask.Common.Networking
{
    public class FusionNetworkService : INetworkService, INetworkRunnerCallbacks, IDisposable
    {
        readonly IInputService _inputService;
        readonly PlayerAvatar _avatarPrefab;
        NetworkRunner _runner;
        bool _started;
        readonly HashSet<NetworkObject> _spawned = new();
        readonly Subject<Unit> _sessionEnded = new();
        public bool IsHost => _runner != null && _runner.IsServer;
        public PlayerRef LocalPlayer => _runner?.LocalPlayer ?? default;
        public NetworkRunner Runner => _runner;
        public event Action<NetworkRunner, PlayerRef> PlayerJoined;
        public event Action<NetworkRunner, PlayerRef> PlayerLeft;
        public Subject<Unit> SessionEnded => _sessionEnded;

        [Inject]
        public FusionNetworkService(IInputService inputService, PlayerAvatar playerAvatarPrefab)
        {
            _inputService = inputService;
            _avatarPrefab = playerAvatarPrefab;
        }

        public async UniTask<bool> StartGameAsync(string sessionName, string playerName)
        {
            if (_started) return false;
            _started = true;
            _runner = new GameObject(nameof(FusionNetworkService)).AddComponent<NetworkRunner>();
            Object.DontDestroyOnLoad(_runner.gameObject);

            var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            var result = await _runner.StartGame(new StartGameArgs {
                GameMode      = GameMode.AutoHostOrClient,
                SessionName   = sessionName,
                Scene         = sceneRef,
                SceneManager  = _runner.SceneManager
            });

            if (!result.Ok) {
                Cleanup();
                return false;
            }

            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            return true;
        }

        public void Shutdown()
        {
            if (_runner == null) return;
            if (_runner.IsServer) {
                foreach (var o in _spawned) _runner.Despawn(o);
            }
            _spawned.Clear();
            _runner.StartCoroutine(CleanupNextFrame());
        }

        IEnumerator CleanupNextFrame()
        {
            yield return null;
            if (!_runner) yield break;
            _runner.RemoveCallbacks(this);
            _runner.Shutdown();
            Object.Destroy(_runner.gameObject);
            _runner = null;
            _started = false;
            _sessionEnded.OnNext(Unit.Default);
        }

        void Cleanup()
        {
            if (_runner != null) {
                Object.Destroy(_runner.gameObject);
                _runner = null;
            }
            _started = false;
        }

        public void Dispose() => Shutdown();

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var d = new PlayerInputData {
                Steer    = _inputService.Steer,
                Throttle = _inputService.Throttle
            };
            input.Set(d);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef pr)
        {
            if (runner.IsServer && !runner.TryGetPlayerObject(pr, out _)) {
                var obj = runner.Spawn(_avatarPrefab, Vector3.zero, Quaternion.identity, pr);
                _spawned.Add(obj.Object);
                runner.SetPlayerObject(pr, obj.Object);
            }
            PlayerJoined?.Invoke(runner, pr);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef pr)
        {
            if (runner.IsServer && runner.TryGetPlayerObject(pr, out var o)) {
                runner.Despawn(o);
                _spawned.Remove(o);
            }
            PlayerLeft?.Invoke(runner, pr);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            _sessionEnded.OnNext(Unit.Default);
            if (_runner != null) _runner.StartCoroutine(CleanupNextFrame());
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            _sessionEnded.OnNext(Unit.Default);
            if (_runner != null) _runner.StartCoroutine(CleanupNextFrame());
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
    }
}