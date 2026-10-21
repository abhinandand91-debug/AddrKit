using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>
    /// Draws a rule set as one card per rule, with a live preview of the addresses it produces.
    /// Used by the AddrKit window (Rules tab) and by the rule set's own Inspector, so both look the same.
    /// </summary>
    internal sealed class RuleSetView
    {
        sealed class Preview
        {
            public int Count;
            public List<string> Samples = new List<string>();
            public double Time;
        }

        readonly Dictionary<string, Preview> previews = new Dictionary<string, Preview>();
        string labelsEditingKey;

        public void Draw(AddrRuleSet set)
        {
            var serialized = new SerializedObject(set);
            serialized.Update();
            SerializedProperty rules = serialized.FindProperty("rules");

            if (rules.arraySize == 0)
            {
                using (new AddrGUI.CardScope())
                {
                    GUILayout.Label("No rules yet", AddrGUI.RowTitle);
                    GUILayout.Label("A rule says: everything in a folder goes into a group, gets an address and labels.\n" +
                                    "Select a folder in the Project window and press \"Add rule for selected folder\".", AddrGUI.Hint);
                }
            }

            int remove = -1, moveUp = -1, moveDown = -1;
            for (int i = 0; i < rules.arraySize; i++)
                DrawRule(rules.GetArrayElementAtIndex(i), i, rules.arraySize, ref remove, ref moveUp, ref moveDown);

            if (remove >= 0) rules.DeleteArrayElementAtIndex(remove);
            if (moveUp > 0) rules.MoveArrayElement(moveUp, moveUp - 1);
            if (moveDown >= 0 && moveDown < rules.arraySize - 1) rules.MoveArrayElement(moveDown, moveDown + 1);

            if (serialized.ApplyModifiedProperties()) previews.Clear();
        }

        void DrawRule(SerializedProperty rule, int index, int count, ref int remove, ref int moveUp, ref int moveDown)
        {
            SerializedProperty enabled = rule.FindPropertyRelative("enabled");
            SerializedProperty folder = rule.FindPropertyRelative("folder");
            SerializedProperty group = rule.FindPropertyRelative("groupName");

            using (new AddrGUI.CardScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    enabled.boolValue = EditorGUILayout.Toggle(enabled.boolValue, GUILayout.Width(16));
                    using (new EditorGUI.DisabledScope(!enabled.boolValue))
                        EditorGUILayout.PropertyField(folder, GUIContent.none);
                    using (new EditorGUI.DisabledScope(index == 0))
                        if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(22))) moveUp = index;
                    using (new EditorGUI.DisabledScope(index == count - 1))
                        if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(22))) moveDown = index;
                    if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(22))) remove = index;
                }

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Group", GUILayout.Width(44));
                        group.stringValue = EditorGUILayout.TextField(group.stringValue);
                        if (rule.FindPropertyRelative("remote").boolValue) AddrGUI.DrawPill("remote", Tone.Info);
                    }
                    DrawPreview(rule);
                }

                rule.isExpanded = EditorGUILayout.Foldout(rule.isExpanded, "Options", true);
                if (rule.isExpanded) DrawOptions(rule);
            }
        }

        void DrawPreview(SerializedProperty rule)
        {
            var folderObject = rule.FindPropertyRelative("folder").objectReferenceValue;
            if (folderObject == null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    AddrGUI.DrawPill("No folder", Tone.Warn);
                    GUILayout.Label("Drag a folder from the Project window into the field above.", AddrGUI.Hint);
                }
                return;
            }

            Preview preview = GetPreview(rule);
            using (new EditorGUILayout.HorizontalScope())
            {
                AddrGUI.DrawPill($"{preview.Count} asset{(preview.Count == 1 ? "" : "s")}", preview.Count > 0 ? Tone.Good : Tone.Warn);
                string text = preview.Count == 0
                    ? "Nothing matches. Check the folder and the file types under Options."
                    : string.Join("   ", preview.Samples) + (preview.Count > preview.Samples.Count ? $"   +{preview.Count - preview.Samples.Count} more" : "");
                GUILayout.Label(text, AddrGUI.Hint);
            }
        }

        void DrawOptions(SerializedProperty rule)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(rule.FindPropertyRelative("addressMode"));
            EditorGUILayout.PropertyField(rule.FindPropertyRelative("addressPrefix"));
            DrawLabels(rule.FindPropertyRelative("labels"), rule.propertyPath);
            EditorGUILayout.PropertyField(rule.FindPropertyRelative("extensions"));
            EditorGUILayout.PropertyField(rule.FindPropertyRelative("includeSubfolders"));
            EditorGUILayout.PropertyField(rule.FindPropertyRelative("packing"));
            EditorGUILayout.PropertyField(rule.FindPropertyRelative("remote"));
            EditorGUI.indentLevel--;
        }

        /// <summary>Labels as one comma-separated field: easier to type than a list.</summary>
        void DrawLabels(SerializedProperty labels, string id)
        {
            var current = new List<string>();
            for (int i = 0; i < labels.arraySize; i++) current.Add(labels.GetArrayElementAtIndex(i).stringValue);

            string joined = string.Join(", ", current);
            EditorGUI.BeginChangeCheck();
            string edited = EditorGUILayout.TextField(new GUIContent("Labels", "Comma-separated, e.g. enemy, level1"), joined);
            if (!EditorGUI.EndChangeCheck()) return;

            string[] parts = edited.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            labels.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++) labels.GetArrayElementAtIndex(i).stringValue = parts[i];
        }

        Preview GetPreview(SerializedProperty rule)
        {
            var model = new AddrRule
            {
                folder = rule.FindPropertyRelative("folder").objectReferenceValue as DefaultAsset,
                extensions = rule.FindPropertyRelative("extensions").stringValue,
                includeSubfolders = rule.FindPropertyRelative("includeSubfolders").boolValue,
                addressPrefix = rule.FindPropertyRelative("addressPrefix").stringValue,
                addressMode = (AddressMode)rule.FindPropertyRelative("addressMode").enumValueIndex
            };

            string key = $"{model.FolderPath}|{model.extensions}|{model.includeSubfolders}|{model.addressPrefix}|{model.addressMode}";
            if (previews.TryGetValue(key, out Preview cached) && EditorApplication.timeSinceStartup - cached.Time < 2.0)
                return cached;

            List<string> paths = RuleApplier.FindAssets(model);
            var preview = new Preview { Count = paths.Count, Time = EditorApplication.timeSinceStartup };
            foreach (string path in paths.Take(3))
                preview.Samples.Add(RuleApplier.ComputeAddress(model.addressMode, model.addressPrefix, model.FolderPath, path));

            previews[key] = preview;
            return preview;
        }
    }
}
