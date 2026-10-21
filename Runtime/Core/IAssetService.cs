using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace AddrKit
{
    /// <summary>
    /// What AddrKit does, as an interface. <see cref="Addr"/> is a static shortcut to one instance.
    /// Replace it with <c>Addr.Service = ...</c> in tests or when you want dependency injection.
    ///
    /// Keys are addresses, labels or AssetReference runtime keys.
    /// An "owner" is anything: a MonoBehaviour, a GameObject, an <see cref="AddrScope"/>.
    /// When the owner is a destroyed Unity object, everything it loaded is released.
    /// Passing null makes the load global: it stays until you release it.
    ///
    /// If the owner is released while a load is still running, the load is abandoned: the task completes
    /// with null (or an empty list) and nothing stays in memory. If the CancellationToken is cancelled, the load is
    /// abandoned the same way and the task is cancelled (OperationCanceledException). See Documentation~/Lifecycle.md.
    /// </summary>
    public interface IAssetService
    {
        Task<T> Load<T>(object key, object owner = null, CancellationToken cancellation = default) where T : class;
        Task<IList<T>> LoadAll<T>(object keyOrLabel, object owner = null, CancellationToken cancellation = default) where T : class;
        Task<GameObject> Spawn(object key, InstantiationParameters parameters, object owner = null, CancellationToken cancellation = default);
        Task<IList<GameObject>> SpawnAll(object label, InstantiationParameters parameters, object owner = null, CancellationToken cancellation = default);
        Task<SceneInstance> LoadScene(object key, bool additive, object owner = null, CancellationToken cancellation = default);
        Task UnloadScene(object key);

        void Despawn(GameObject instance);
        void Unload(UnityEngine.Object asset);
        void ReleaseOwner(object owner);
        void ReleaseAll();

        Task Preload(object keyOrLabel, Action<float> onProgress = null, CancellationToken cancellation = default);
        Task<long> GetDownloadSize(object keyOrLabel);
        Task<int> UpdateCatalogs();

        IReadOnlyList<LoadRecord> Snapshot();
    }
}
