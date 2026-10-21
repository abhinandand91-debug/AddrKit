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

            var panel = new Rect(Screen.width / scale - width - 10f, 10f, width, height);
            GUI.DrawTexture(panel, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.08f, 0.09f, 0.11f, 0.93f), 0, 6);

            GUILayout.BeginArea(new Rect(panel.x + 8, panel.y + 6, panel.width - 16, panel.height - 12));
            var text = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, normal = { textColor = new Color(0.86f, 0.88f, 0.9f) } };

            int global = 0;
            foreach (LoadRecord r in records) if (r.OwnerName == "Global") global++;
            GUILayout.Label($"<b>AddrKit</b>   {records.Count} live" + (global > 0 ? $"   <color=#ffb454>{global} global</color>" : "") +
                            $"   <color=#7d8590>hide: {AddrSettings.Current.overlayKey}</color>", text);

            scroll = GUILayout.BeginScrollView(scroll);
            foreach (LoadRecord r in records)
            {
                string owner = r.OwnerName == "Global" ? "<color=#ffb454>Global</color>" : r.OwnerName;
                GUILayout.Label($"<color=#7d8590>{r.Kind}</color>  <b>{r.Key}</b>\n<color=#7d8590>owner</color> {owner}   <color=#7d8590>{r.Age:0.0}s</color>", text);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
