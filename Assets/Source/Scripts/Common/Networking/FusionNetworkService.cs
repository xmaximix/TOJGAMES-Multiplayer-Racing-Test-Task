using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Addons.Physics;
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
        private readonly IInputService inputService;
        private readonly PlayerAvatar avatarPrefab;

        private NetworkRunner runner;
        private bool started;
        private readonly HashSet<NetworkObject> spawned = new();

        private readonly Subject<Unit> sessionEnded = new();
        private readonly Subject<(NetworkRunner runner, PlayerRef player)> playerJoined = new();
        private readonly Subject<(NetworkRunner runner, PlayerRef player)> playerLeft = new();

        public bool IsHost => runner != null && runner.IsServer;
        public PlayerRef LocalPlayer => runner?.LocalPlayer ?? default;
        public NetworkRunner Runner => runner;
        public Subject<Unit> SessionEnded => sessionEnded;
        public Subject<(NetworkRunner runner, PlayerRef player)> PlayerJoined => playerJoined;
        public Subject<(NetworkRunner runner, PlayerRef player)> PlayerLeft => playerLeft;
        private readonly Dictionary<PlayerRef,string> nickMap = new();
        public IReadOnlyDictionary<PlayerRef,string> Nicknames => nickMap;

        
        public FusionNetworkService(
            IInputService inputService,
            PlayerAvatar playerAvatarPrefab)
        {
            this.inputService = inputService;
            avatarPrefab = playerAvatarPrefab;
        }

        public async UniTask<PlayerAvatar> GetAvatarAsync(PlayerRef player)
        {
            await UniTask.WaitUntil(() =>
                runner.TryGetPlayerObject(player, out var netObj) && netObj != null);
            var avatar = runner.GetPlayerObject(player).GetComponent<PlayerAvatar>();
            avatar.SetNetworkService(this);
            return avatar;
        }

        public async UniTask<bool> StartGameAsync(string sessionName, string playerName)
        {
            if (started)
                return false;

            started = true;
            InitializeRunner();

            var args = new StartGameArgs
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = sessionName,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = runner.SceneManager
            };

            var result = await runner.StartGame(args);
            if (!result.Ok)
            {
                DestroyRunner();
                return false;
            }

            runner.ProvideInput = true;
            runner.AddCallbacks(this);
            
            return true;
        }

        public void Shutdown()
        {
            if (runner == null)
                return;

            if (runner.IsServer)
            {
                foreach (var netObj in spawned)
                    runner.Despawn(netObj);
            }

            spawned.Clear();
            runner.StartCoroutine(CleanupCoroutine());
        }

        private IEnumerator CleanupCoroutine()
        {
            yield return null;

            if (runner == null)
                yield break;

            runner.RemoveCallbacks(this);
            runner.Shutdown();
            DestroyRunner();
            sessionEnded.OnNext(Unit.Default);
        }

        private void InitializeRunner()
        {
            var go = new GameObject(nameof(FusionNetworkService));
            runner = go.AddComponent<NetworkRunner>();
            var physSim = runner.gameObject.AddComponent<RunnerSimulatePhysics3D>();
            physSim.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;  
            Object.DontDestroyOnLoad(go);
        }
        
        public void RegisterNickname(PlayerRef pr, string nick)
        {
            if (!string.IsNullOrEmpty(nick))
                 nickMap[pr] = nick;
        }

        private void DestroyRunner()
        {
            if (runner == null)
                return;

            Object.Destroy(runner.gameObject);
            runner = null;
            started = false;
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            input.Set(new PlayerInputData
            {
                steer = inputService.Steer,
                throttle = inputService.Throttle
            });
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                if (!runner.TryGetPlayerObject(player, out _))
                {
                    var obj = runner.Spawn(
                        avatarPrefab,
                        Vector3.zero,
                        Quaternion.identity,
                        player
                    );
                    var netObj = obj.GetComponent<NetworkObject>();
                    spawned.Add(netObj);
                    runner.SetPlayerObject(player, netObj);
                }
            } 
            playerJoined.OnNext((runner, player));
        }
        
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer && runner.TryGetPlayerObject(player, out var netObj))
            {
                runner.Despawn(netObj);
                spawned.Remove(netObj);
            }

            playerLeft.OnNext((runner, player));
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
            sessionEnded.OnNext(Unit.Default);
            if (runner != null)
                runner.StartCoroutine(CleanupCoroutine());
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