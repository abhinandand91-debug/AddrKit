using UnityEngine;

namespace AddrKit
{
    /// <summary>No-code scene loading by address, with optional unload when this object is destroyed.</summary>
    [AddComponentMenu("AddrKit/Scene Loader")]
    public sealed class AddrSceneLoader : MonoBehaviour
    {
        [AddrKey] public string sceneKey;
        public bool loadOnStart = true;
        public bool additive = true;
        [Tooltip("Unload the scene when this object is destroyed.")]
        public bool unloadOnDestroy = true;

        bool loaded;

        void Start()
        {
            if (loadOnStart) Load();
        }

        public async void Load()
        {
            if (loaded || string.IsNullOrEmpty(sceneKey)) return;
            try
            {
                loaded = true;
                await Addr.LoadScene(sceneKey, additive);
            }
            catch (AddrException e)
            {
                loaded = false;
                Debug.LogError(e.Message, this);
            }
        }

        void OnDestroy()
        {
            if (loaded && unloadOnDestroy && !Addr.IsQuitting) _ = Addr.UnloadScene(sceneKey);
        }
    }
}
