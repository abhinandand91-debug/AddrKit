using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Edit the rule set and apply it. Shows the same inspector you get by selecting the asset.</summary>
    internal sealed class RulesTab : IAddrTab
    {
        UnityEditor.Editor editor;
        AddrRuleSet shown;

        public string Title => "Rules";

        public void Draw(AddrKitWindow window)
        {
            var ruleSet = AddrEditorUtil.FindFirst<AddrRuleSet>();
            if (ruleSet == null)
            {
                EditorGUILayout.HelpBox("No rule set yet. Run Setup, or create one with Assets > Create > AddrKit > Rule Set.", MessageType.Warning);
                if (GUILayout.Button("Run Setup")) AddrKitSetup.RunSetup();
                return;
            }

            if (shown != ruleSet || editor == null)
            {
                Object.DestroyImmediate(editor);
                editor = UnityEditor.Editor.CreateEditor(ruleSet);
                shown = ruleSet;
            }

            if (GUILayout.Button("Add rule for the selected folder"))
                AddRuleForSelection(ruleSet);

            EditorGUILayout.Space();
            editor.OnInspectorGUI();
        }

        static void AddRuleForSelection(AddrRuleSet ruleSet)
        {
            var folder = Selection.activeObject as DefaultAsset;
            string path = folder != null ? AssetDatabase.GetAssetPath(folder) : null;
            if (path == null || !AssetDatabase.IsValidFolder(path))
            {
                EditorUtility.DisplayDialog("AddrKit", "Select a folder in the Project window first.", "OK");
                return;
            }

            Undo.RecordObject(ruleSet, "Add AddrKit rule");
            ruleSet.rules.Add(new AddrRule { folder = folder, groupName = folder.name });
            EditorUtility.SetDirty(ruleSet);
        }
    }
}
