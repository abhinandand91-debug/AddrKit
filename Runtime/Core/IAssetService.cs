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
    /// </summary>
    public interface IAssetService
    {
        Task<T> Load<T>(object key, object owner = null) where T : class;
        Task<IList<T>> LoadAll<T>(object keyOrLabel, object owner = null) where T : class;
        Task<GameObject> Spawn(object key, InstantiationParameters parameters, object owner = null);
        Task<SceneInstance> LoadScene(object key, bool additive, object owner = null);
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
