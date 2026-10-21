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
    }
}
