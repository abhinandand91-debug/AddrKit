using System.Threading.Tasks;
using UnityEngine;

namespace AddrKit
{
    /// <summary>
    /// No-code spawning: pick an address and this component instantiates it and releases it
    /// when this GameObject is destroyed.
    /// </summary>
    [AddComponentMenu("AddrKit/Spawner")]
    public sealed class AddrSpawner : MonoBehaviour
    {
        [AddrKey] public string key;
        [Tooltip("Spawn automatically in Start. Otherwise call Spawn() from code or a UnityEvent.")]
        public bool spawnOnStart = true;
        [Tooltip("Parent the spawned object to this GameObject (otherwise it spawns at this position in the scene root).")]
        public bool asChild = true;

        /// <summary>The spawned object, or null before it finishes loading.</summary>
        public GameObject Spawned { get; private set; }

        async void Start()
        {
            if (spawnOnStart) await Spawn();
        }

        public async Task<GameObject> Spawn()
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[AddrKit] AddrSpawner has no key assigned.", this);
                return null;
            }

            try
            {
                Spawned = asChild
                    ? await Addr.SpawnUnder(key, transform, this)
                    : await Addr.Spawn(key, transform.position, transform.rotation, this);
            }
            catch (AddrException e)
            {
                Debug.LogError(e.Message, this);
            }
            return Spawned;
        }
    }
}
