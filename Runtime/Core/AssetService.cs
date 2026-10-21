using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace AddrKit
{
    /// <summary>
    /// Default <see cref="IAssetService"/>. Every load is wrapped in a <see cref="LoadRecord"/>
    /// that remembers its owner, so releasing is one call (or automatic).
    /// </summary>
    public sealed class AssetService : IAssetService
    {
        /// <summary>Owner used when the caller passes null.</summary>
        sealed class GlobalOwner
        {
            public override string ToString() => "Global";
        }

        static readonly object Global = new GlobalOwner();

        readonly List<LoadRecord> records = new List<LoadRecord>();

        // ---------------------------------------------------------------- loading

        public async Task<T> Load<T>(object key, object owner = null, CancellationToken cancellation = default) where T : class
        {
            cancellation.ThrowIfCancellationRequested();
            var handle = Addressables.LoadAssetAsync<T>(key);
            var record = Track(key, typeof(T), LoadKind.Asset, handle, owner);

            T result = await Complete(handle, record, key, cancellation);
            if (record.Released) return null;
            record.Asset = result;
            return result;
        }

        public async Task<IList<T>> LoadAll<T>(object keyOrLabel, object owner = null, CancellationToken cancellation = default) where T : class
        {
            cancellation.ThrowIfCancellationRequested();
            var handle = Addressables.LoadAssetsAsync<T>(keyOrLabel, (Action<T>)null);
            var record = Track(keyOrLabel, typeof(T), LoadKind.Assets, handle, owner);

            IList<T> result = await Complete(handle, record, keyOrLabel, cancellation);
            return record.Released ? new List<T>() : result;
        }

        public async Task<GameObject> Spawn(object key, InstantiationParameters parameters, object owner = null, CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            var handle = Addressables.InstantiateAsync(key, parameters);
            var record = Track(key, typeof(GameObject), LoadKind.Instance, handle, owner);

            GameObject instance = await Complete(handle, record, key, cancellation);
            if (record.Released) return null;
            record.Instance = instance;
            return instance;
        }

        public async Task<IList<GameObject>> SpawnAll(object label, InstantiationParameters parameters, object owner = null, CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();

            // Find every address under the label, then spawn each through the normal tracked path.
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
            IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation> locations = await locationsHandle.Task;
            var keys = new List<string>();
            foreach (var location in locations) keys.Add(location.PrimaryKey);
            Addressables.Release(locationsHandle);

            var tasks = new List<Task<GameObject>>();
            foreach (string key in keys) tasks.Add(Spawn(key, parameters, owner, cancellation));
            GameObject[] spawned = await Task.WhenAll(tasks);

            var result = new List<GameObject>();
            foreach (GameObject go in spawned) if (go != null) result.Add(go);
            return result;
        }

        public async Task<SceneInstance> LoadScene(object key, bool additive, object owner = null, CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            var mode = additive ? UnityEngine.SceneManagement.LoadSceneMode.Additive
                                : UnityEngine.SceneManagement.LoadSceneMode.Single;
            var handle = Addressables.LoadSceneAsync(key, mode);
            var record = Track(key, typeof(SceneInstance), LoadKind.Scene, handle, owner);

            return await Complete(handle, record, key, cancellation);
        }

        public async Task UnloadScene(object key)
        {
            LoadRecord record = records.Find(r => r.Kind == LoadKind.Scene && Equals(r.Key, key?.ToString()));
            if (record == null)
            {
                AddrLog.Warn($"UnloadScene: no scene loaded through AddrKit with key '{key}'.");
                return;
            }

            record.Released = true;
            records.Remove(record);
            if (record.Handle.IsValid()) await Addressables.UnloadSceneAsync(record.Handle).Task;
        }

        // ---------------------------------------------------------------- releasing

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            LoadRecord record = records.Find(r => r.Instance == instance);
            if (record == null)
            {
                AddrLog.Warn($"Despawn: '{instance.name}' was not spawned by AddrKit. Destroying it normally.");
                Object.Destroy(instance);
                return;
            }
            ReleaseRecord(record);
        }

        public void Unload(Object asset)
        {
            if (asset == null) return;

            LoadRecord record = records.Find(r => ReferenceEquals(r.Asset, asset));
            if (record == null)
            {
                AddrLog.Warn($"Unload: '{asset.name}' was not loaded by AddrKit.");
                return;
            }
            ReleaseRecord(record);
        }

        public void ReleaseOwner(object owner)
        {
            object target = owner ?? Global;
            // Copy first: releasing mutates the list.
            foreach (LoadRecord record in records.FindAll(r => ReferenceEquals(r.Owner, target)))
                ReleaseRecord(record);
        }

        public void ReleaseAll()
        {
            foreach (LoadRecord record in records.ToArray())
                ReleaseRecord(record);
        }

        // ---------------------------------------------------------------- downloading

        public async Task Preload(object keyOrLabel, Action<float> onProgress = null, CancellationToken cancellation = default)
        {
            var handle = Addressables.DownloadDependenciesAsync(keyOrLabel, false);
            try
            {
                while (!handle.IsDone)
                {
                    cancellation.ThrowIfCancellationRequested();
                    onProgress?.Invoke(handle.PercentComplete);
                    await Task.Yield();
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                    throw AddrException.ForKey(keyOrLabel, handle.OperationException);

                onProgress?.Invoke(1f);
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        public async Task<long> GetDownloadSize(object keyOrLabel)
        {
            var handle = Addressables.GetDownloadSizeAsync(keyOrLabel);
            try
            {
                long size = await handle.Task;
                return handle.Status == AsyncOperationStatus.Succeeded ? size : 0;
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        public async Task<int> UpdateCatalogs()
        {
            var check = Addressables.CheckForCatalogUpdates(false);
            List<string> catalogs = await check.Task;
            int count = catalogs?.Count ?? 0;

            if (count > 0)
            {
                var update = Addressables.UpdateCatalogs(catalogs);
                await update.Task;
            }
            Addressables.Release(check);
            return count;
        }

        public IReadOnlyList<LoadRecord> Snapshot() => records.ToArray();

        // ---------------------------------------------------------------- internals

        LoadRecord Track(object key, Type type, LoadKind kind, AsyncOperationHandle handle, object owner)
        {
            object resolved = owner ?? Global;
            var record = new LoadRecord
            {
                Key = key?.ToString() ?? "<null>",
                AssetType = type,
                Kind = kind,
                Owner = resolved,
                OwnerName = DescribeOwner(resolved),
                Handle = handle,
                LoadedAtTime = Time.realtimeSinceStartup,
                StackTrace = CaptureStack()
            };
            records.Add(record);

            AddrOwnerWatcher.WatchIfUnityObject(resolved);
            AddrLog.Verbose($"Load started: {record}");
            return record;
        }

        /// <summary>
        /// Waits for a handle.
        /// - Released meanwhile (owner destroyed, scope disposed): returns default; the release already cleaned up.
        /// - Cancelled: releases and throws OperationCanceledException.
        /// - Failed: releases and throws a readable AddrException.
        /// </summary>
        async Task<TResult> Complete<TResult>(AsyncOperationHandle<TResult> handle, LoadRecord record, object key, CancellationToken cancellation)
        {
            TResult result = default;
            Exception error = null;

            // Cancelling is just "release this record now"; the checks below then see it was released.
            using (cancellation.Register(() => ReleaseRecord(record)))
            {
                try { result = await handle.Task; }
                catch (Exception e) { error = e; }
            }

            // A released handle is invalid: do not touch it.
            if (record.Released)
            {
                cancellation.ThrowIfCancellationRequested();
                AddrLog.Verbose($"{record} finished after its owner was released; result discarded.");
                return default;
            }

            if (error != null || handle.Status != AsyncOperationStatus.Succeeded)
            {
                ReleaseRecord(record);
                throw AddrException.ForKey(key, error ?? handle.OperationException);
            }

            return result;
        }

        void ReleaseRecord(LoadRecord record)
        {
            if (record.Released) return;
            record.Released = true;
            records.Remove(record);

            switch (record.Kind)
            {
                case LoadKind.Instance:
                    if (record.Instance != null) Addressables.ReleaseInstance(record.Instance);
                    else if (record.Handle.IsValid()) Addressables.Release(record.Handle);
                    break;

                case LoadKind.Scene:
                    if (record.Handle.IsValid()) Addressables.UnloadSceneAsync(record.Handle);
                    break;

                default:
                    if (record.Handle.IsValid()) Addressables.Release(record.Handle);
                    break;
            }
            AddrLog.Verbose($"Released: {record}");
        }

        static string DescribeOwner(object owner)
        {
            if (owner is Object unityObject)
                return unityObject != null ? $"{unityObject.name} ({unityObject.GetType().Name})" : "<destroyed>";
            return owner.ToString();
        }

        static string CaptureStack()
        {
            if (!AddrSettings.Current.trackStackTraces) return null;
            if (!Application.isEditor && !Debug.isDebugBuild) return null;
            return new System.Diagnostics.StackTrace(3, true).ToString();
        }
    }
}
