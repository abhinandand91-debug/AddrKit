using System.Collections.Generic;
using UnityEngine;

namespace AddrKit
{
    /// <summary>
    /// On-screen list of everything AddrKit currently has loaded. Press the overlay key
    /// (F9 by default) to show it. Created automatically in the Editor and development builds.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class AddrDebugOverlay : MonoBehaviour
    {
        bool visible;
        Vector2 scroll;
        IReadOnlyList<LoadRecord> records = new List<LoadRecord>();
        float nextRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (!AddrSettings.Current.createDebugOverlay) return;
            if (FindFirstObjectByType<AddrDebugOverlay>() != null) return;

            var go = new GameObject("AddrKit Overlay") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<AddrDebugOverlay>();
        }

        void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == AddrSettings.Current.overlayKey)
            {
                visible = !visible;
                e.Use();
            }
            if (!visible) return;

            if (Time.unscaledTime >= nextRefresh)
            {
                records = Addr.Snapshot();
                nextRefresh = Time.unscaledTime + 0.25f;
            }

            float scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Mathf.Min(520f, Screen.width / scale - 20f);
            float height = Screen.height / scale - 20f;

            GUILayout.BeginArea(new Rect(Screen.width / scale - width - 10f, 10f, width, height), GUI.skin.box);
            GUILayout.Label($"<b>AddrKit</b>  live loads: {records.Count}   (hide: {AddrSettings.Current.overlayKey})",
                new GUIStyle(GUI.skin.label) { richText = true });

            scroll = GUILayout.BeginScrollView(scroll);
            foreach (LoadRecord r in records)
                GUILayout.Label($"{r.Kind,-8} {r.Key}  [{r.AssetType?.Name}]  owner: {r.OwnerName}  {r.Age:0.0}s  {r.Status}");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
