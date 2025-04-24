using System;
using System.Collections;
using System.Collections.Generic;
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
        private readonly IInputService _inputService;
        private readonly PlayerAvatar _avatarPrefab;

        private NetworkRunner _runner;
        private bool _started;
        private readonly HashSet<NetworkObject> _spawned = new();

        private readonly Subject<Unit> _sessionEnded = new();
        private readonly Subject<(NetworkRunner runner, PlayerRef player)> _playerJoined = new();
        private readonly Subject<(NetworkRunner runner, PlayerRef player)> _playerLeft = new();

        public bool IsHost => _runner != null && _runner.IsServer;
        public PlayerRef LocalPlayer => _runner?.LocalPlayer ?? default;
        public NetworkRunner Runner => _runner;
        public Subject<Unit> SessionEnded => _sessionEnded;
        public Subject<(NetworkRunner runner, PlayerRef player)> PlayerJoined => _playerJoined;
        public Subject<(NetworkRunner runner, PlayerRef player)> PlayerLeft => _playerLeft;
        
        [Inject]
        public FusionNetworkService(
            IInputService inputService,
            PlayerAvatar playerAvatarPrefab)
        {
            _inputService = inputService;
            _avatarPrefab = playerAvatarPrefab;
        }

        public async UniTask<PlayerAvatar> GetAvatarAsync(PlayerRef player)
        {
            await UniTask.WaitUntil(() =>
                _runner.TryGetPlayerObject(player, out var netObj) && netObj != null);
            var avatar = _runner.GetPlayerObject(player).GetComponent<PlayerAvatar>();
            return avatar;
        }

        public async UniTask<bool> StartGameAsync(string sessionName, string playerName)
        {
            if (_started)
                return false;

            _started = true;
            InitializeRunner();

            var args = new StartGameArgs
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = sessionName,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = _runner.SceneManager
            };

            var result = await _runner.StartGame(args);
            if (!result.Ok)
            {
                DestroyRunner();
                return false;
            }

            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            return true;
        }

        public void Shutdown()
        {
            if (_runner == null)
                return;

            if (_runner.IsServer)
            {
                foreach (var netObj in _spawned)
                    _runner.Despawn(netObj);
            }

            _spawned.Clear();
            _runner.StartCoroutine(CleanupCoroutine());
        }

        private IEnumerator CleanupCoroutine()
        {
            yield return null;

            if (_runner == null)
                yield break;

            _runner.RemoveCallbacks(this);
            _runner.Shutdown();
            Object.Destroy(_runner.gameObject);

            _runner = null;
            _started = false;

            _sessionEnded.OnNext(Unit.Default);
        }

        private void InitializeRunner()
        {
            var go = new GameObject(nameof(FusionNetworkService));
            _runner = go.AddComponent<NetworkRunner>();
            Object.DontDestroyOnLoad(go);
        }

        private void DestroyRunner()
        {
            if (_runner == null)
                return;

            Object.Destroy(_runner.gameObject);
            _runner = null;
            _started = false;
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            input.Set(new PlayerInputData
            {
                Steer = _inputService.Steer,
                Throttle = _inputService.Throttle
            });
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer && !runner.TryGetPlayerObject(player, out _))
            {
                var obj = runner.Spawn(_avatarPrefab, Vector3.zero, Quaternion.identity, player);
                _spawned.Add(obj.Object);
                runner.SetPlayerObject(player, obj.Object);
            }
            
            _playerJoined.OnNext((runner, player));
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer && runner.TryGetPlayerObject(player, out var netObj))
            {
                runner.Despawn(netObj);
                _spawned.Remove(netObj);
            }

            _playerLeft.OnNext((runner, player));
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            EndSession();
        }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            EndSession();
        }

        private void EndSession()
        {
            _sessionEnded.OnNext(Unit.Default);
            if (_runner != null)
                _runner.StartCoroutine(CleanupCoroutine());
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
        }
        public void OnConnectedToServer(NetworkRunner runner)
        {
        }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
        }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
        }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken token)
        {
        }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }
        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }
        public void OnSceneLoadDone(NetworkRunner runner)
        {
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}