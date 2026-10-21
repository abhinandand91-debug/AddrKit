using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace AddrKit
{
    /// <summary>
    /// The one class most games need. Short, readable calls on top of Addressables.
    ///
    /// <code>
    /// var goblin = await Addr.Spawn(Keys.Enemies.Goblin, owner: this);
    /// var icon   = await Addr.Load&lt;Sprite&gt;(Keys.Icons.Sword, owner: this);
    /// await Addr.Preload(Labels.Level1, p => bar.value = p);
    /// </code>
    ///
    /// <b>Owner</b>: pass <c>this</c> (or an <see cref="AddrScope"/>) and everything loaded is released
    /// automatically when the owner is destroyed. Pass nothing for a global load, released by hand.
    /// </summary>
    public static class Addr
    {
        static IAssetService service;

        /// <summary>The engine behind this class. Replace it for tests or custom setups.</summary>
        public static IAssetService Service
        {
            get => service ??= new AssetService();
            set => service = value;
        }

        /// <summary>True once the application starts shutting down. Releases are skipped then.</summary>
        public static bool IsQuitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            service = null;
            IsQuitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        static void OnQuitting() => IsQuitting = true;

        // ------------------------------------------------------------ setup

        /// <summary>Optional. Addressables initializes itself on first use; call this to do it during a splash screen.</summary>
        public static Task Initialize() => Addressables.InitializeAsync().Task;

        /// <summary>True if the key (address or label) exists in a loaded catalog.</summary>
        public static bool Exists(string key)
        {
            foreach (var locator in Addressables.ResourceLocators)
                if (locator.Locate(key, typeof(object), out _)) return true;
            return false;
        }

        // ------------------------------------------------------------ loading assets

        /// <summary>Loads one asset (sprite, audio clip, prefab asset, ScriptableObject...).</summary>
        public static Task<T> Load<T>(string key, object owner = null, CancellationToken cancellation = default) where T : class
            => Service.Load<T>(key, owner, cancellation);

        /// <summary>Loads one asset from an Inspector AssetReference.</summary>
        public static Task<T> Load<T>(AssetReference reference, object owner = null, CancellationToken cancellation = default) where T : class
            => Service.Load<T>(RuntimeKey(reference), owner, cancellation);

        /// <summary>Loads every asset that has the label (or address) and returns them all.</summary>
        public static Task<IList<T>> LoadAll<T>(string labelOrKey, object owner = null, CancellationToken cancellation = default) where T : class
            => Service.LoadAll<T>(labelOrKey, owner, cancellation);

        // ------------------------------------------------------------ spawning

        /// <summary>Instantiates a prefab at its own position.</summary>
        public static Task<GameObject> Spawn(string key, object owner = null, CancellationToken cancellation = default)
            => Service.Spawn(key, new InstantiationParameters(null, false), owner, cancellation);

        /// <summary>Instantiates a prefab at a world position.</summary>
        public static Task<GameObject> Spawn(string key, Vector3 position, Quaternion rotation, object owner = null, Transform parent = null, CancellationToken cancellation = default)
            => Service.Spawn(key, new InstantiationParameters(position, rotation, parent), owner, cancellation);

        /// <summary>Instantiates a prefab as a child (local transform of the prefab is kept).</summary>
        public static Task<GameObject> SpawnUnder(string key, Transform parent, object owner = null, CancellationToken cancellation = default)
            => Service.Spawn(key, new InstantiationParameters(parent, false), owner, cancellation);

        /// <summary>
        /// Instantiates every prefab that has the label, each tracked by the owner like a normal Spawn.
        /// Prefer this over LoadAll + Instantiate, which leaves the instances untracked.
        /// </summary>
        public static Task<IList<GameObject>> SpawnAll(string label, object owner = null, Transform parent = null, CancellationToken cancellation = default)
            => Service.SpawnAll(label, new InstantiationParameters(parent, false), owner, cancellation);

        /// <summary>Instantiates from an Inspector AssetReference.</summary>
        public static Task<GameObject> Spawn(AssetReference reference, Vector3 position, Quaternion rotation, object owner = null, Transform parent = null, CancellationToken cancellation = default)
            => Service.Spawn(RuntimeKey(reference), new InstantiationParameters(position, rotation, parent), owner, cancellation);

        // ------------------------------------------------------------ scenes

        /// <summary>Loads a scene by address. Scenes are global by default; unload them with <see cref="UnloadScene"/>.</summary>
        public static Task<SceneInstance> LoadScene(string key, bool additive = true, object owner = null, CancellationToken cancellation = default)
            => Service.LoadScene(key, additive, owner, cancellation);

        public static Task UnloadScene(string key) => Service.UnloadScene(key);

        // ------------------------------------------------------------ releasing

        /// <summary>Destroys an instance spawned by AddrKit and releases its memory.</summary>
        public static void Despawn(GameObject instance) => Service.Despawn(instance);

        /// <summary>Releases an asset returned by <see cref="Load{T}(string, object)"/>.</summary>
        public static void Unload(UnityEngine.Object asset) => Service.Unload(asset);

        /// <summary>Releases everything loaded with this owner. Automatic when the owner is destroyed.</summary>
        public static void ReleaseAll(object owner) => Service.ReleaseOwner(owner);

        /// <summary>Releases everything AddrKit has loaded, whoever owns it. Handy when returning to a main menu.</summary>
        public static void ReleaseEverything() => Service.ReleaseAll();

        // ------------------------------------------------------------ downloads

        /// <summary>
        /// Downloads everything the key or label needs into the disk cache, reporting download progress from 0 to 1.
        /// It does NOT load anything into memory and keeps no handle, so there is nothing to release.
        /// Already-cached or local content completes immediately. Failure throws <see cref="AddrException"/>;
        /// cancelling throws OperationCanceledException and stops reporting.
        /// </summary>
        public static Task Preload(string labelOrKey, Action<float> onProgress = null, CancellationToken cancellation = default)
            => Service.Preload(labelOrKey, onProgress, cancellation);

        /// <summary>Bytes still to download for this key or label. Zero means local or already cached.</summary>
        public static Task<long> GetDownloadSize(string labelOrKey) => Service.GetDownloadSize(labelOrKey);

        /// <summary>Checks the remote server for new content. Returns how many catalogs were updated.</summary>
        public static Task<int> UpdateCatalogs() => Service.UpdateCatalogs();

        // ------------------------------------------------------------ diagnostics

        /// <summary>Everything currently loaded through AddrKit.</summary>
        public static IReadOnlyList<LoadRecord> Snapshot() => Service.Snapshot();

        static object RuntimeKey(AssetReference reference)
        {
            if (reference == null || !reference.RuntimeKeyIsValid())
                throw new AddrException("<AssetReference>", "The AssetReference is empty. Assign an asset in the Inspector.");
            return reference.RuntimeKey;
        }
    }
}
