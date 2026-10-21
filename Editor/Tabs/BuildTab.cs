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
            AddrGUI.Header("Build", "How content loads in the Editor, building bundles, and testing downloadable content.");

            var settings = AddrEditorUtil.AddressablesSettings;
            if (settings == null)
            {
                using (new AddrGUI.CardScope("Not set up yet"))
                    if (AddrGUI.PrimaryButton("Set Up AddrKit")) AddrKitSetup.RunSetup();
                return;
            }

            DrawPlayModeScript(settings);
            DrawBuild();
            DrawRemote(settings);
        }

        static void DrawPlayModeScript(AddressableAssetSettings settings)
        {
            using (new AddrGUI.CardScope("Play mode script"))
            {
                GUILayout.Label("How Addressables loads when you press Play in the Editor.", AddrGUI.Hint);

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

                string active = names.Count > 0 ? names[Mathf.Clamp(chosen, 0, names.Count - 1)] : "";
                GUILayout.Label(active.Contains("Existing")
                    ? "Loads real bundles. Build content first. Use this to test what players get, and for remote content."
                    : "Reads assets straight from the project. Fastest, no build needed. Use it day to day.", AddrGUI.Hint);
            }
        }

        static void DrawBuild()
        {
            using (new AddrGUI.CardScope("Content"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (AddrGUI.PrimaryButton("Build Content", 30)) AddrKitSetup.BuildContent();
                    if (AddrGUI.Button("Clean Build Cache", 30, GUILayout.Width(130))) AddressableAssetSettings.CleanPlayerContent();
                }
                GUILayout.Label("Build content before making a player build, and before testing with \"Use Existing Build\".", AddrGUI.Hint);
            }
        }

        void DrawRemote(AddressableAssetSettings settings)
        {
            using (new AddrGUI.CardScope("Remote content (downloadable)"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    AddrGUI.DrawPill(settings.BuildRemoteCatalog ? "Remote on" : "Remote off", settings.BuildRemoteCatalog ? Tone.Info : Tone.Neutral);
                    AddrGUI.DrawPill(LocalContentServer.IsRunning ? $"Server :{LocalContentServer.Port}" : "Server stopped",
                        LocalContentServer.IsRunning ? Tone.Good : Tone.Neutral);
                }
                GUILayout.Label("1  Tick \"remote\" on a rule.   2  Turn remote on.   3  Build Content.   4  Start the server and press Play with \"Use Existing Build\".", AddrGUI.Hint);
                GUILayout.Space(4);

                remoteUrl = remoteUrl ?? "http://localhost:" + port;
                remoteUrl = EditorGUILayout.TextField("Server URL", remoteUrl);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (AddrGUI.Button("Turn Remote On", 24)) RemoteSetup.Configure(settings, true, remoteUrl);
                    if (AddrGUI.Button("Turn Remote Off", 24)) RemoteSetup.Configure(settings, false, remoteUrl);
                }

                AddrGUI.Separator();
                port = EditorGUILayout.IntField("Local server port", port);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(LocalContentServer.IsRunning))
                        if (AddrGUI.Button("Start Local Server", 24))
                        {
                            System.IO.Directory.CreateDirectory(RemoteSetup.ServerFolder);
                            LocalContentServer.Start(RemoteSetup.ServerFolder, port);
                        }
                    using (new EditorGUI.DisabledScope(!LocalContentServer.IsRunning))
                        if (AddrGUI.Button("Stop Server", 24)) LocalContentServer.Stop();
                }
                GUILayout.Label($"Serves the {RemoteSetup.ServerFolder}/ folder. For production, upload it to your CDN and put that URL above.", AddrGUI.Hint);
            }
        }
    }
}
