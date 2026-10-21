using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>
    /// Live list of everything AddrKit has loaded while the game runs, with owner, age and where it was loaded from.
    /// A row that never goes away is a leak.
    /// </summary>
    internal sealed class DebugTab : IAddrTab
    {
        readonly HashSet<LoadRecord> expanded = new HashSet<LoadRecord>();
        string search = "";
        int kindFilter;   // 0 = all

        static readonly string[] KindNames = { "All", "Asset", "Assets", "Instance", "Scene" };

        public string Title => "Debug";

        public void Draw(AddrKitWindow window)
        {
            AddrGUI.Header("Debug", "Everything loaded through AddrKit right now. Loads that never go away are leaks.");

            if (!Application.isPlaying)
            {
                using (new AddrGUI.CardScope("Press Play"))
                {
                    GUILayout.Label("This page fills in while the game runs. You can also press F9 in the Game view for an on-screen version.", AddrGUI.Hint);
                    GUILayout.Space(2);
                    GUILayout.Label("What to look for:", AddrGUI.RowTitle);
                    GUILayout.Label("•  a row with owner \"Global\" that you did not expect\n•  a scope that is still alive after its level ended\n•  rows piling up each time you open a screen", AddrGUI.Hint);
                }
                return;
            }

            IReadOnlyList<LoadRecord> all = Addr.Snapshot();
            List<LoadRecord> shown = all.Where(Matches).ToList();

            using (new EditorGUILayout.HorizontalScope())
            {
                AddrGUI.DrawPill($"{all.Count} live", all.Count == 0 ? Tone.Neutral : Tone.Info);
                int global = all.Count(r => r.OwnerName == "Global");
                if (global > 0) AddrGUI.DrawPill($"{global} global", Tone.Warn);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Copy report", EditorStyles.miniButton, GUILayout.Width(80))) EditorGUIUtility.systemCopyBuffer = BuildReport(all);
                if (GUILayout.Button("Release everything", EditorStyles.miniButton, GUILayout.Width(124))) Addr.ReleaseEverything();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                kindFilter = EditorGUILayout.Popup(kindFilter, KindNames, GUILayout.Width(80));
            }
            GUILayout.Space(4);

            if (shown.Count == 0)
            {
                GUILayout.Label(all.Count == 0 ? "Nothing is loaded." : "No loads match the filter.", AddrGUI.Hint);
                return;
            }

            foreach (LoadRecord record in shown) DrawRecord(record);
        }

        bool Matches(LoadRecord record)
        {
            if (kindFilter > 0 && record.Kind.ToString() != KindNames[kindFilter]) return false;
            if (string.IsNullOrWhiteSpace(search)) return true;
            string needle = search.Trim();
            return record.Key.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0
                || record.OwnerName.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void DrawRecord(LoadRecord record)
        {
            using (new AddrGUI.CardScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(record.Key, AddrGUI.RowTitle);
                    GUILayout.FlexibleSpace();
                    AddrGUI.DrawPill(record.Kind.ToString(), Tone.Neutral);
                    AddrGUI.DrawPill(record.Status, record.Status == "Succeeded" ? Tone.Good : Tone.Warn);
                }
                GUILayout.Label($"{record.AssetType?.Name}   ·   owner: {record.OwnerName}   ·   {record.Age:0.0}s",
                    record.OwnerName == "Global" ? new GUIStyle(AddrGUI.Hint) { fontStyle = FontStyle.Bold } : AddrGUI.Hint);

                if (string.IsNullOrEmpty(record.StackTrace)) return;
                bool open = expanded.Contains(record);
                bool now = EditorGUILayout.Foldout(open, "Loaded from", true);
                if (now) { expanded.Add(record); GUILayout.Label(record.StackTrace.Trim(), AddrGUI.Hint); }
                else expanded.Remove(record);
            }
        }

        static string BuildReport(IReadOnlyList<LoadRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"AddrKit report: {records.Count} live load(s), Unity {Application.unityVersion}, t={Time.realtimeSinceStartup:0.0}s");
            foreach (LoadRecord r in records)
            {
                sb.AppendLine($"- {r.Kind} '{r.Key}' [{r.AssetType?.Name}] owner={r.OwnerName} age={r.Age:0.0}s status={r.Status}");
                if (!string.IsNullOrEmpty(r.StackTrace)) sb.AppendLine("    " + r.StackTrace.Trim().Replace("\n", "\n    "));
            }
            return sb.ToString();
        }
    }
}
