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
        private LobbySystem system;
        private FakeNetwork net;

        sealed class FakeNetwork : INetworkService
        {
            private Subject<(NetworkRunner runner, PlayerRef player)> playerJoined;
            private Subject<(NetworkRunner runner, PlayerRef player)> playerLeft;
            public NetworkRunner Runner { get; }
            public Subject<Unit> SessionEnded { get; } = new();
            public Subject<(NetworkRunner runner, PlayerRef player)> PlayerJoined { get; } = new();
            public Subject<(NetworkRunner runner, PlayerRef player)> PlayerLeft { get; }= new();
            public IReadOnlyDictionary<PlayerRef, string> Nicknames => names;
            private readonly Dictionary<PlayerRef, string> names = new();
            public bool IsHost { get; set; }
            public PlayerRef LocalPlayer => PlayerRef.FromIndex(0);
            public UniTask<bool> StartGameAsync(string a, string b) => UniTask.FromResult(true);

            public FakeNetwork()
            {
                var go = new GameObject("FakeRunner");
                Runner = go.AddComponent<NetworkRunner>();
            }

            public void Shutdown() => SessionEnded.OnNext(Unit.Default);
            public void RegisterNickname(PlayerRef pr, string nick)
            {
            }

            private readonly Dictionary<PlayerRef, PlayerAvatar> avatars = new();

            public UniTask<PlayerAvatar> GetAvatarAsync(PlayerRef pr) =>
                avatars.TryGetValue(pr, out var av)
                    ? UniTask.FromResult(av)
                    : UniTask.FromCanceled<PlayerAvatar>(new CancellationToken(true));

            public void RegisterAvatar(PlayerRef pr, string name)
            {
                var go = new GameObject($"Avatar_{pr.RawEncoded}");
                var no = go.AddComponent<NetworkObject>();
                Runner.SetPlayerObject(pr, no);

                var avatar = go.AddComponent<PlayerAvatar>();
                avatar.NameChanged.OnNext((pr, name));

                avatars[pr] = avatar;
            }

            public void RaiseJoin(PlayerRef pr) => PlayerJoined?.OnNext((Runner, pr));
            public void RaiseLeft(PlayerRef pr) => PlayerLeft?.OnNext((Runner, pr));
        }

        [SetUp]
        public void SetUp()
        {
            net = new FakeNetwork { IsHost = true };
            system = new LobbySystem(net);
        }

        [TearDown]
        public void TearDown()
        {
            system.Dispose();
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
            net.RegisterAvatar(p, $"Player {p.RawEncoded}");
            net.RaiseJoin(p);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(system.Players.Count, Is.EqualTo(1));
            Assert.That(system.Players[0].Id,   Is.EqualTo(p));
            Assert.That(system.Players[0].Name, Is.EqualTo($"Player {p.RawEncoded}"));
        }

        [UnityTest]
        public IEnumerator DuplicateJoin_Ignored()
        {
            var p = PlayerRef.FromIndex(7);
            net.RegisterAvatar(p, $"Player {p.RawEncoded}");
            net.RaiseJoin(p);
            net.RaiseJoin(p);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(system.Players.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RaiseLeft_RemovesOnlyThatPlayer()
        {
            var a = PlayerRef.FromIndex(1);
            var b = PlayerRef.FromIndex(2);

            net.RegisterAvatar(a, "A");
            net.RegisterAvatar(b, "B");
            net.RaiseJoin(a);
            net.RaiseJoin(b);

            yield return UniTask.NextFrame().ToCoroutine();

            net.RaiseLeft(a);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(system.Players.Count, Is.EqualTo(1));
            Assert.That(system.Players[0].Id, Is.EqualTo(b));
        }

        [UnityTest]
        public IEnumerator LeaveNonExisting_DoesNothing()
        {
            net.RaiseLeft(PlayerRef.FromIndex(99));

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(system.Players.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesFromEvents()
        {
            var p = PlayerRef.FromIndex(55);
            system.Dispose();

            net.RegisterAvatar(p, "X");
            net.RaiseJoin(p);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(system.Players.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Commands_EmitButDoNotChangeState()
        {
            int started = 0;
            system.StartCommand.Subscribe(_ => started++);
            system.StartCommand.Execute(Unit.Default);

            yield return UniTask.NextFrame().ToCoroutine();

            Assert.That(started, Is.EqualTo(1));
            Assert.That(system.Players.Count, Is.Zero);
        }
    }
}