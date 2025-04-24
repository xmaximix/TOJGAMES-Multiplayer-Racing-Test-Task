// Editor_Tests/Modules/Lobby/LobbySystemTests.cs

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using R3;
using TojGamesTask.Common.Networking;
using TojGamesTask.Modules.Lobby.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace Editor_Tests.Modules.Lobby
{
    [TestFixture]
    public class LobbySystemTests
    {
        private LobbySystem _system;
        private FakeNetwork _net;

        sealed class FakeNetwork : INetworkService
        {
            private Subject<(NetworkRunner runner, PlayerRef player)> playerJoined;
            private Subject<(NetworkRunner runner, PlayerRef player)> playerLeft;
            public NetworkRunner Runner { get; }
            public Subject<Unit> SessionEnded { get; } = new();
            public Subject<(NetworkRunner runner, PlayerRef player)> PlayerJoined { get; } = new();
            public Subject<(NetworkRunner runner, PlayerRef player)> PlayerLeft { get; }= new();
            public bool IsHost { get; set; }
            public PlayerRef LocalPlayer => PlayerRef.FromIndex(0);
            public UniTask<bool> StartGameAsync(string a, string b) => UniTask.FromResult(true);

            public FakeNetwork()
            {
                var go = new GameObject("FakeRunner");
                Runner = go.AddComponent<NetworkRunner>();
            }

            public void Shutdown() => SessionEnded.OnNext(Unit.Default);

            private readonly Dictionary<PlayerRef, PlayerAvatar> _avatars = new();

            public UniTask<PlayerAvatar> GetAvatarAsync(PlayerRef pr) =>
                _avatars.TryGetValue(pr, out var av)
                    ? UniTask.FromResult(av)
                    : UniTask.FromCanceled<PlayerAvatar>(new CancellationToken(true));

            public void RegisterAvatar(PlayerRef pr, string name)
            {
                var go = new GameObject($"Avatar_{pr.RawEncoded}");
                var no = go.AddComponent<NetworkObject>();
                Runner.SetPlayerObject(pr, no);

                var avatar = go.AddComponent<PlayerAvatar>();
                avatar.NameChanged.OnNext((pr, name));

                _avatars[pr] = avatar;
            }

            public void RaiseJoin(PlayerRef pr) => PlayerJoined?.OnNext((Runner, pr));
            public void RaiseLeft(PlayerRef pr) => PlayerLeft?.OnNext((Runner, pr));
        }

        [SetUp]
        public void SetUp()
        {
            _net = new FakeNetwork { IsHost = true };
            _system = new LobbySystem(_net);
        }

        [TearDown]
        public void TearDown()
        {
            _system.Dispose();
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("Avatar_") || go.name == "FakeRunner")
                    Object.DestroyImmediate(go);
            }
        }

        [UnityTest]
        public IEnumerator RaiseJoin_PlayerAppearsInList()
        {
            var p = PlayerRef.FromIndex(42);
            _net.RegisterAvatar(p, $"Player {p.RawEncoded}");
            _net.RaiseJoin(p);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(_system.Players.Count, Is.EqualTo(1));
            Assert.That(_system.Players[0].Id,   Is.EqualTo(p));
            Assert.That(_system.Players[0].Name, Is.EqualTo($"Player {p.RawEncoded}"));
        }

        [UnityTest]
        public IEnumerator DuplicateJoin_Ignored()
        {
            var p = PlayerRef.FromIndex(7);
            _net.RegisterAvatar(p, $"Player {p.RawEncoded}");
            _net.RaiseJoin(p);
            _net.RaiseJoin(p);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(_system.Players.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RaiseLeft_RemovesOnlyThatPlayer()
        {
            var a = PlayerRef.FromIndex(1);
            var b = PlayerRef.FromIndex(2);

            _net.RegisterAvatar(a, "A");
            _net.RegisterAvatar(b, "B");
            _net.RaiseJoin(a);
            _net.RaiseJoin(b);

            yield return UniTask.NextFrame().ToCoroutine();

            _net.RaiseLeft(a);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(_system.Players.Count, Is.EqualTo(1));
            Assert.That(_system.Players[0].Id, Is.EqualTo(b));
        }

        [UnityTest]
        public IEnumerator LeaveNonExisting_DoesNothing()
        {
            _net.RaiseLeft(PlayerRef.FromIndex(99));

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(_system.Players.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesFromEvents()
        {
            var p = PlayerRef.FromIndex(55);
            _system.Dispose();

            _net.RegisterAvatar(p, "X");
            _net.RaiseJoin(p);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(_system.Players.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Commands_EmitButDoNotChangeState()
        {
            int started = 0;
            _system.StartCommand.Subscribe(_ => started++);
            _system.StartCommand.Execute(Unit.Default);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(started, Is.EqualTo(1));
            Assert.That(_system.Players.Count, Is.Zero);
        }
    }
}