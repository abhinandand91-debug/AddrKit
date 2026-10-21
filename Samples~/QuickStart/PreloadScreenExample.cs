using AddrKit;
using UnityEngine;

namespace AddrKit.Samples
{
    /// <summary>
    /// A loading screen: download everything with a label, show progress, then continue.
    /// For local content this finishes instantly; for remote content it downloads and caches.
    /// </summary>
    public class PreloadScreenExample : MonoBehaviour
    {
        [Tooltip("Label (or address) to download, e.g. 'level1'.")]
        public string labelToPreload = "level1";

        float progress;
        bool done;

        async void Start()
        {
            long bytes = await Addr.GetDownloadSize(labelToPreload);
            Debug.Log($"Need to download {bytes / 1024f:0.#} KB");

            await Addr.Preload(labelToPreload, p => progress = p);
            done = true;
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 300, 60), GUI.skin.box);
            GUILayout.Label(done ? "Ready!" : $"Loading... {progress * 100f:0}%");
            GUILayout.EndArea();
        }
    }
}
