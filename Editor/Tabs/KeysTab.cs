using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>The generated Keys class: status, options, and a preview of what it contains.</summary>
    internal sealed class KeysTab : IAddrTab
    {
        UnityEditor.Editor settingsEditor;
        Vector2 previewScroll;

        public string Title => "Keys";

        public void Draw(AddrKitWindow window)
        {
            AddrGUI.Header("Keys", "Turn addresses into code. Addr.Spawn(Keys.Enemies.Goblin) instead of a string, so a typo fails to compile.");

            var addr = AddrEditorUtil.FindAddrSettings();
            var settings = AddrEditorUtil.AddressablesSettings;
            if (addr == null || settings == null)
            {
                using (new AddrGUI.CardScope("Not set up yet"))
                {
                    if (AddrGUI.PrimaryButton("Set Up AddrKit")) AddrKitSetup.RunSetup();
                }
                return;
            }

            string source = KeyGenerator.BuildSourceFromProject(settings, addr);
            bool exists = File.Exists(addr.keysOutputPath);
            bool upToDate = exists && File.ReadAllText(addr.keysOutputPath) == source;
            int keyCount = source.Split('\n').Count(l => l.Contains("public const string"));

            using (new AddrGUI.CardScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    AddrGUI.DrawPill(!exists ? "Not generated" : upToDate ? "Up to date" : "Out of date", !exists || !upToDate ? Tone.Warn : Tone.Good);
                    AddrGUI.DrawPill($"{keyCount} constants", Tone.Neutral);
                    GUILayout.Label(addr.keysOutputPath, AddrGUI.Hint);
                    GUILayout.FlexibleSpace();
                    if (exists && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(addr.keysOutputPath);
                    if (GUILayout.Button(upToDate ? "Regenerate" : "Generate now", EditorStyles.miniButton, GUILayout.Width(86)))
                        AddrKitSetup.GenerateKeys();
                }
            }

            using (new AddrGUI.CardScope("Options"))
            {
                if (settingsEditor == null || settingsEditor.target != addr)
                    settingsEditor = UnityEditor.Editor.CreateEditor(addr);

                settingsEditor.serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                foreach (string name in new[] { "autoGenerateKeys", "keysOutputPath", "keysNamespace", "autoApplyRules" })
                    EditorGUILayout.PropertyField(settingsEditor.serializedObject.FindProperty(name));
                if (EditorGUI.EndChangeCheck()) settingsEditor.serializedObject.ApplyModifiedProperties();
            }

            using (new AddrGUI.CardScope("Preview"))
            {
                previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.MinHeight(180), GUILayout.MaxHeight(360));
                EditorGUILayout.SelectableLabel(source, AddrGUI.Mono, GUILayout.ExpandHeight(true),
                    GUILayout.MinHeight(source.Split('\n').Length * 15f));
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
