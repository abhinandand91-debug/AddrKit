using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Inspector for a rule set: the rule list plus Apply and a live "N assets match" preview.</summary>
    [CustomEditor(typeof(AddrRuleSet))]
    public sealed class AddrRuleSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var set = (AddrRuleSet)target;

            EditorGUILayout.HelpBox(
                "Each rule says: everything in this folder goes into this group with these labels. " +
                "Press Apply after editing (it also runs automatically when assets change).",
                MessageType.Info);

            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                foreach (AddrRule rule in set.rules)
                {
                    if (rule.folder == null) continue;
                    int count = RuleApplier.FindAssets(rule).Count;
                    EditorGUILayout.LabelField($"{(rule.enabled ? "" : "(off) ")}{rule.FolderPath}  →  {rule.groupName}",
                        $"{count} asset(s)");
                }
            }

            if (GUILayout.Button("Apply Rules", GUILayout.Height(28)))
                AddrKitSetup.ApplyRules();
        }
    }
}
