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

        public async Task<T> Load<T>(object key, object owner = null) where T : class
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            var record = Track(key, typeof(T), LoadKind.Asset, handle, owner);

            T result = await Complete(handle, record, key);
            record.Asset = result;
            return record.Released ? null : result;
        }

        public async Task<IList<T>> LoadAll<T>(object keyOrLabel, object owner = null) where T : class
        {
            var handle = Addressables.LoadAssetsAsync<T>(keyOrLabel, (Action<T>)null);
            var record = Track(keyOrLabel, typeof(T), LoadKind.Assets, handle, owner);

            IList<T> result = await Complete(handle, record, keyOrLabel);
            return record.Released ? new List<T>() : result;
        }

        public async Task<GameObject> Spawn(object key, InstantiationParameters parameters, object owner = null)
        {
            var handle = Addressables.InstantiateAsync(key, parameters);
            var record = Track(key, typeof(GameObject), LoadKind.Instance, handle, owner);

            GameObject instance = await Complete(handle, record, key);
            record.Instance = instance;
            return record.Released ? null : instance;
        }

        public async Task<SceneInstance> LoadScene(object key, bool additive, object owner = null)
        {
            var mode = additive ? UnityEngine.SceneManagement.LoadSceneMode.Additive
                                : UnityEngine.SceneManagement.LoadSceneMode.Single;
            var handle = Addressables.LoadSceneAsync(key, mode);
            var record = Track(key, typeof(SceneInstance), LoadKind.Scene, handle, owner);

            return await Complete(handle, record, key);
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

        /// <summary>Waits for a handle. On failure the record is dropped and a readable exception is thrown.</summary>
        async Task<TResult> Complete<TResult>(AsyncOperationHandle<TResult> handle, LoadRecord record, object key)
        {
            TResult result = default;
            Exception error = null;

            try { result = await handle.Task; }
            catch (Exception e) { error = e; }

            if (error != null || handle.Status != AsyncOperationStatus.Succeeded)
            {
                ReleaseRecord(record);
                throw AddrException.ForKey(key, error ?? handle.OperationException);
            }

            if (record.Released)
                AddrLog.Verbose($"{record} finished after its owner was released; result discarded.");
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
