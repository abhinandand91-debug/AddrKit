using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AddrKit.Tests
{
    [PrebuildSetup("AddrKit.Tests.TestContent")]
    [PostBuildCleanup("AddrKit.Tests.TestContent")]
    public class AddrKitPlayModeTests
    {
        AssetService service;

        [SetUp]
        public void SetUp()
        {
            service = new AssetService();
            Addr.Service = service;
        }

        [TearDown]
        public void TearDown()
        {
            service.ReleaseAll();
            Addr.Service = null;
        }

        /// <summary>Lets a UnityTest wait for a Task and rethrow its exception.</summary>
        static IEnumerator Wait(Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }

        [UnityTest]
        public IEnumerator Load_ReturnsAsset_AndTracksIt()
        {
            var task = Addr.Load<GameObject>(TestKeys.Cube);
            yield return Wait(task);

            Assert.IsNotNull(task.Result);
            Assert.AreEqual(1, Addr.Snapshot().Count);
            Assert.AreEqual(LoadKind.Asset, Addr.Snapshot()[0].Kind);
        }

        [UnityTest]
        public IEnumerator ReleaseAll_Owner_RemovesOnlyThatOwnersLoads()
        {
            var ownerA = new AddrScope("A");
            var ownerB = new AddrScope("B");
            yield return Wait(Addr.Load<GameObject>(TestKeys.Cube, ownerA));
            yield return Wait(Addr.Load<GameObject>(TestKeys.Sphere, ownerB));

            ownerA.Dispose();

            Assert.AreEqual(1, Addr.Snapshot().Count);
            Assert.AreEqual("Scope 'B'", Addr.Snapshot()[0].OwnerName);
        }

        [UnityTest]
        public IEnumerator Spawn_CreatesInstance_AndDespawnDestroysIt()
        {
            var task = Addr.Spawn(TestKeys.Cube, new Vector3(1, 2, 3), Quaternion.identity);
            yield return Wait(task);
            GameObject instance = task.Result;

            Assert.IsNotNull(instance);
            Assert.AreEqual(new Vector3(1, 2, 3), instance.transform.position);

            Addr.Despawn(instance);
            yield return null;

            Assert.IsTrue(instance == null, "Instance should be destroyed");
            Assert.AreEqual(0, Addr.Snapshot().Count);
        }

        [UnityTest]
        public IEnumerator DestroyingTheOwner_ReleasesItsLoads()
        {
            var owner = new GameObject("Owner");
            yield return Wait(Addr.Spawn(TestKeys.Cube, Vector3.zero, Quaternion.identity, owner));
            Assert.AreEqual(1, Addr.Snapshot().Count);

            UnityEngine.Object.Destroy(owner);
            yield return null;
            yield return null;

            Assert.AreEqual(0, Addr.Snapshot().Count, "Loads should be released when the owner is destroyed");
        }

        [UnityTest]
        public IEnumerator Scope_Dispose_ReleasesEverythingInIt()
        {
            using (var scope = new AddrScope("Level"))
            {
                yield return Wait(Addr.Spawn(TestKeys.Cube, Vector3.zero, Quaternion.identity, scope));
                yield return Wait(Addr.Spawn(TestKeys.Sphere, Vector3.zero, Quaternion.identity, scope));
                Assert.AreEqual(2, Addr.Snapshot().Count);
            }
            Assert.AreEqual(0, Addr.Snapshot().Count);
        }

        [UnityTest]
        public IEnumerator LoadAll_ByLabel_ReturnsEveryAsset()
        {
            var task = Addr.LoadAll<GameObject>(TestKeys.Label);
            yield return Wait(task);

            Assert.AreEqual(2, task.Result.Count);
            Assert.AreEqual(1, Addr.Snapshot().Count, "One handle covers the whole label");
        }

        [UnityTest]
        public IEnumerator Unload_ReleasesTheAsset()
        {
            var task = Addr.Load<GameObject>(TestKeys.Cube);
            yield return Wait(task);

            Addr.Unload(task.Result);

            Assert.AreEqual(0, Addr.Snapshot().Count);
        }

        [UnityTest]
        public IEnumerator MissingKey_Throws_WithDidYouMean()
        {
            LogAssert.ignoreFailingMessages = true;   // Addressables also logs the failure itself
            var task = Addr.Load<GameObject>("tests/cub");
            while (!task.IsCompleted) yield return null;
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(task.IsFaulted);
            var error = task.Exception.InnerException as AddrException;
            Assert.IsNotNull(error, "Expected an AddrException");
            StringAssert.Contains("tests/cube", error.Message);
            Assert.AreEqual(0, Addr.Snapshot().Count, "A failed load must not stay tracked");
        }

        [UnityTest]
        public IEnumerator Exists_FindsKnownAndUnknownKeys()
        {
            yield return Wait(Addr.Initialize());

            Assert.IsTrue(Addr.Exists(TestKeys.Cube));
            Assert.IsFalse(Addr.Exists("nope/nothing"));
        }

        [UnityTest]
        public IEnumerator Preload_ReportsProgress_AndCompletes()
        {
            float last = -1f;
            yield return Wait(Addr.Preload(TestKeys.Label, p => last = p));

            Assert.AreEqual(1f, last, 0.0001f);
        }

        [UnityTest]
        public IEnumerator GetDownloadSize_IsZeroForLocalContent()
        {
            var task = Addr.GetDownloadSize(TestKeys.Cube);
            yield return Wait(task);

            Assert.AreEqual(0L, task.Result);
        }

        // ---- lifecycle contract (from the review findings)

        [UnityTest]
        public IEnumerator Spawn_OwnerReleasedMidLoad_ReturnsNull_AndLeavesNothing()
        {
            int before = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;
            var scope = new AddrScope("Mid");
            var task = Addr.Spawn(TestKeys.Cube, Vector3.zero, Quaternion.identity, scope);
            if (task.IsCompleted) Assert.Inconclusive("Load finished synchronously; cannot test in-flight release.");

            scope.Dispose();                       // owner goes away while the load is in flight
            while (!task.IsCompleted) yield return null;
            yield return null;

            Assert.IsFalse(task.IsFaulted, task.Exception?.InnerException?.Message);
            Assert.IsNull(task.Result);
            Assert.AreEqual(0, Addr.Snapshot().Count);
            Assert.AreEqual(before, UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,
                "No instance should be left in the scene");
        }

        [UnityTest]
        public IEnumerator Load_ScopeDisposedMidLoad_DoesNotThrow()
        {
            var scope = new AddrScope("Mid");
            var task = Addr.Load<GameObject>(TestKeys.Cube, scope);
            if (task.IsCompleted) Assert.Inconclusive("Load finished synchronously.");

            scope.Dispose();
            while (!task.IsCompleted) yield return null;

            Assert.IsFalse(task.IsFaulted, task.Exception?.InnerException?.Message);
            Assert.AreEqual(0, Addr.Snapshot().Count);
        }

        [UnityTest]
        public IEnumerator Dispose_Twice_IsSafe()
        {
            var scope = new AddrScope("Twice");
            yield return Wait(Addr.Load<GameObject>(TestKeys.Cube, scope));

            scope.Dispose();
            Assert.DoesNotThrow(() => scope.Dispose());
            Assert.AreEqual(0, Addr.Snapshot().Count);
        }

        [UnityTest]
        public IEnumerator SharedAsset_ReleasingOneOwner_KeepsItAliveForTheOther()
        {
            var a = new AddrScope("A");
            var b = new AddrScope("B");
            var taskA = Addr.Load<GameObject>(TestKeys.Cube, a);
            var taskB = Addr.Load<GameObject>(TestKeys.Cube, b);
            yield return Wait(taskA);
            yield return Wait(taskB);

            a.Dispose();
            yield return null;

            Assert.IsTrue(taskB.Result != null, "B's asset must survive A's release");
            Assert.AreEqual(1, Addr.Snapshot().Count);
        }

        [UnityTest]
        public IEnumerator RepeatedSpawnDespawn_LeavesNoObjectsOrRecords()
        {
            int before = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;
            for (int i = 0; i < 20; i++)
            {
                var task = Addr.Spawn(TestKeys.Sphere, Vector3.zero, Quaternion.identity);
                yield return Wait(task);
                Addr.Despawn(task.Result);
                yield return null;
            }

            Assert.AreEqual(0, Addr.Snapshot().Count);
            Assert.AreEqual(before, UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator Load_Cancelled_IsCancelled_AndLeavesNothing()
        {
            var cts = new System.Threading.CancellationTokenSource();
            var task = Addr.Load<GameObject>(TestKeys.Cube, null, cts.Token);
            if (task.IsCompleted) Assert.Inconclusive("Load finished synchronously.");

            cts.Cancel();
            while (!task.IsCompleted) yield return null;

            Assert.IsTrue(task.IsCanceled, "Cancelled loads must surface as cancelled tasks");
            Assert.AreEqual(0, Addr.Snapshot().Count);
        }

        [Test]
        public void Spawn_WithAlreadyCancelledToken_ThrowsImmediately()
        {
            var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();

            var task = Addr.Spawn(TestKeys.Cube, null, cts.Token);

            Assert.IsTrue(task.IsCanceled || task.IsFaulted);
            Assert.AreEqual(0, Addr.Snapshot().Count);
        }

        [UnityTest]
        public IEnumerator SpawnAll_SpawnsEveryPrefabWithTheLabel_AndTracksEach()
        {
            var owner = new AddrScope("All");
            var task = Addr.SpawnAll(TestKeys.Label, owner);
            yield return Wait(task);

            Assert.AreEqual(2, task.Result.Count);
            Assert.AreEqual(2, Addr.Snapshot().Count);

            owner.Dispose();
            yield return null;

            Assert.AreEqual(0, Addr.Snapshot().Count);
            foreach (var go in task.Result) Assert.IsTrue(go == null, "Instances should be destroyed with the scope");
        }
    }
}
