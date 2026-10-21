using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>A checklist of what AddrKit needs, with one button that does it all.</summary>
    internal sealed class SetupTab : IAddrTab
    {
        public string Title => "Setup";

        public void Draw(AddrKitWindow window)
        {
            EditorGUILayout.LabelField("Get started", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "AddrKit sits on top of Unity Addressables. Run Setup once; after that, put assets in folders, " +
                "describe the folders with rules, and load them with Addr.Load / Addr.Spawn.",
                MessageType.Info);

            var addressables = AddrEditorUtil.AddressablesSettings;
            var addr = AddrEditorUtil.FindAddrSettings();
            var ruleSet = AddrEditorUtil.FindFirst<AddrRuleSet>();
            bool keysExist = addr != null && System.IO.File.Exists(addr.keysOutputPath);

            Row("Addressables settings", addressables != null, "Created by Run Setup");
            Row("AddrKit settings asset", addr != null, "Assets/AddrKit/Resources");
            Row("Rule set", ruleSet != null, "Assets/AddrKit/AddrRules.asset");
            Row("Generated Keys class", keysExist, addr != null ? addr.keysOutputPath : "");

            EditorGUILayout.Space();
            bool allDone = addressables != null && addr != null && ruleSet != null && keysExist;
            if (GUILayout.Button(allDone ? "Run Setup again" : "Run Setup", GUILayout.Height(32)))
                AddrKitSetup.RunSetup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Next steps", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1.  Rules tab: add a rule for each content folder.");
            EditorGUILayout.LabelField("2.  Use  Keys.Folder.Asset  in code instead of strings.");
            EditorGUILayout.LabelField("3.  Analyze tab: check for duplicates and big assets.");
            EditorGUILayout.LabelField("4.  Press Play, then F9 in the Game view to see live loads.");
        }

        static void Row(string label, bool ok, string hint)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var icon = EditorGUIUtility.IconContent(ok ? "TestPassed" : "TestFailed");
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(18));
                EditorGUILayout.LabelField(label, GUILayout.Width(180));
                EditorGUILayout.LabelField(ok ? "Ready" : "Missing", EditorStyles.miniLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
            }
        }
    }
}
