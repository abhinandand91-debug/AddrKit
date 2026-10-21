using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Play mode script, content build, and remote content testing.</summary>
    internal sealed class BuildTab : IAddrTab
    {
        string remoteUrl;
        int port = 8080;

        public string Title => "Build";

        public void Draw(AddrKitWindow window)
        {
            var settings = AddrEditorUtil.AddressablesSettings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Run Setup first.", MessageType.Warning);
                return;
            }

            DrawPlayModeScript(settings);
            EditorGUILayout.Space();
            DrawBuild();
            EditorGUILayout.Space();
            DrawRemote(settings);
        }

        // ---- how Addressables loads while you press Play in the Editor
        static void DrawPlayModeScript(AddressableAssetSettings settings)
        {
            EditorGUILayout.LabelField("Play mode script", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use Asset Database: fastest, no build needed (recommended while working).\n" +
                "Use Existing Build: loads real bundles, so build content first. Use it to test the real thing.",
                MessageType.None);

            var names = new List<string>();
            var indices = new List<int>();
            for (int i = 0; i < settings.DataBuilders.Count; i++)
            {
                if (settings.DataBuilders[i] is IDataBuilder builder && builder.CanBuildData<AddressablesPlayModeBuildResult>())
                {
                    names.Add(builder.Name);
                    indices.Add(i);
                }
            }

            int current = Mathf.Max(0, indices.IndexOf(settings.ActivePlayModeDataBuilderIndex));
            int chosen = EditorGUILayout.Popup(current, names.ToArray());
            if (chosen != current) settings.ActivePlayModeDataBuilderIndex = indices[chosen];
        }

        static void DrawBuild()
        {
            EditorGUILayout.LabelField("Content", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Content", GUILayout.Height(26))) AddrKitSetup.BuildContent();
                if (GUILayout.Button("Clean Build Cache", GUILayout.Height(26)))
                    AddressableAssetSettings.CleanPlayerContent();
            }
        }

        // ---- test remote content on this machine
        void DrawRemote(AddressableAssetSettings settings)
        {
            EditorGUILayout.LabelField("Remote content (downloadable)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1) Tick 'remote' on a rule.  2) Turn remote on below.  3) Build Content.  " +
                "4) Start the local server and press Play with 'Use Existing Build'.", MessageType.None);

            remoteUrl ??= "http://localhost:" + port;
            remoteUrl = EditorGUILayout.TextField("Server URL", remoteUrl);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Turn Remote On")) RemoteSetup.Configure(settings, true, remoteUrl);
                if (GUILayout.Button("Turn Remote Off")) RemoteSetup.Configure(settings, false, remoteUrl);
            }

            EditorGUILayout.LabelField("Remote catalog", settings.BuildRemoteCatalog ? "On" : "Off");

            port = EditorGUILayout.IntField("Local server port", port);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(LocalContentServer.IsRunning))
                    if (GUILayout.Button("Start Local Server"))
                    {
                        System.IO.Directory.CreateDirectory(RemoteSetup.ServerFolder);
                        LocalContentServer.Start(RemoteSetup.ServerFolder, port);
                    }
                using (new EditorGUI.DisabledScope(!LocalContentServer.IsRunning))
                    if (GUILayout.Button("Stop Server")) LocalContentServer.Stop();
            }
            EditorGUILayout.LabelField("Server", LocalContentServer.IsRunning
                ? $"Running on port {LocalContentServer.Port}, serving {RemoteSetup.ServerFolder}/"
                : "Stopped");
        }
    }
}
