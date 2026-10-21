using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Live list of everything AddrKit has loaded while the game runs, with where it was loaded from.</summary>
    internal sealed class DebugTab : IAddrTab
    {
        readonly HashSet<LoadRecord> expanded = new HashSet<LoadRecord>();

        public string Title => "Debug";

        public void Draw(AddrKitWindow window)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Press Play to see live loads. A load that never goes away is a leak: " +
                                        "check its owner and open 'Loaded from' to find the line of code.", MessageType.Info);
                return;
            }

            var records = Addr.Snapshot();
            EditorGUILayout.LabelField($"{records.Count} live load(s)", EditorStyles.boldLabel);

            if (GUILayout.Button("Release Everything")) Addr.ReleaseEverything();
            EditorGUILayout.Space();

            foreach (LoadRecord record in records)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"{record.Kind}  '{record.Key}'", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{record.AssetType?.Name}   owner: {record.OwnerName}   {record.Age:0.0}s   {record.Status}",
                        EditorStyles.miniLabel);

                    if (string.IsNullOrEmpty(record.StackTrace)) continue;
                    bool open = expanded.Contains(record);
                    if (EditorGUILayout.Foldout(open, "Loaded from", true)) { expanded.Add(record); EditorGUILayout.TextArea(record.StackTrace, EditorStyles.miniLabel); }
                    else expanded.Remove(record);
                }
            }
        }
    }
}
