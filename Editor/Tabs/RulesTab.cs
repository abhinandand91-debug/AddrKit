using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Rules as cards. Lists every rule set in the project, so none is hidden.</summary>
    internal sealed class RulesTab : IAddrTab
    {
        readonly RuleSetView view = new RuleSetView();

        public string Title => "Rules";

        public void Draw(AddrKitWindow window)
        {
            AddrGUI.Header("Rules", "Tell AddrKit which folders hold your content. Everything inside gets an address, a group and labels.");

            List<AddrRuleSet> sets = AddrEditorUtil.FindAll<AddrRuleSet>();
            if (sets.Count == 0)
            {
                using (new AddrGUI.CardScope("No rule set yet"))
                {
                    GUILayout.Label("Run Setup to create one, or use Assets > Create > AddrKit > Rule Set.", AddrGUI.Hint);
                    if (AddrGUI.PrimaryButton("Run Setup")) AddrKitSetup.RunSetup();
                }
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (AddrGUI.Button("+ Add rule for selected folder", 26)) AddRuleForSelection(sets[0]);
                if (AddrGUI.Button("Apply Rules", 26, GUILayout.Width(110))) AddrKitSetup.ApplyRules();
            }
            GUILayout.Space(4);

            foreach (AddrRuleSet set in sets)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(set.name, AddrGUI.RowTitle);
                    GUILayout.Label(AssetDatabase.GetAssetPath(set), AddrGUI.Hint);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50))) Selection.activeObject = set;
                }
                view.Draw(set);
            }
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
