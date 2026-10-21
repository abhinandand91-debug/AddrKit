using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Settings for the generated Keys class and a preview of what it contains.</summary>
    internal sealed class KeysTab : IAddrTab
    {
        UnityEditor.Editor settingsEditor;
        Vector2 previewScroll;

        public string Title => "Keys";

        public void Draw(AddrKitWindow window)
        {
            var addr = AddrEditorUtil.FindAddrSettings();
            var settings = AddrEditorUtil.AddressablesSettings;
            if (addr == null || settings == null)
            {
                EditorGUILayout.HelpBox("Run Setup first.", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                "Keys turns addresses into code:  Addr.Spawn(Keys.Enemies.Goblin)  instead of a string. " +
                "A typo then fails to compile.", MessageType.Info);

            if (settingsEditor == null || settingsEditor.target != addr)
                settingsEditor = UnityEditor.Editor.CreateEditor(addr);
            settingsEditor.OnInspectorGUI();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Keys Now")) AddrKitSetup.GenerateKeys();
                if (GUILayout.Button("Select File"))
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(addr.keysOutputPath);
            }

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            string source = KeyGenerator.BuildSourceFromProject(settings, addr);
            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.MinHeight(160));
            EditorGUILayout.TextArea(source, EditorStyles.textArea);
            EditorGUILayout.EndScrollView();
        }
    }
}
