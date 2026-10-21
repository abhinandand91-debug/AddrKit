using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>One page of the AddrKit window.</summary>
    internal interface IAddrTab
    {
        string Title { get; }
        void Draw(AddrKitWindow window);
    }

    /// <summary>Tools > AddrKit > Open Window. Setup, rules, keys, analysis, build and live debugging in one place.</summary>
    public sealed class AddrKitWindow : EditorWindow
    {
        const string TabPref = "AddrKit.SelectedTab";

        IAddrTab[] tabs;
        string[] titles;
        int selected;
        Vector2 scroll;
        string version;

        public static void Open()
        {
            var window = GetWindow<AddrKitWindow>("AddrKit");
            window.minSize = new Vector2(460, 420);
        }

        void OnEnable()
        {
            tabs = new IAddrTab[]
            {
                new SetupTab(), new RulesTab(), new KeysTab(), new AnalyzeTab(), new BuildTab(), new DebugTab()
            };
            titles = System.Array.ConvertAll(tabs, t => t.Title);
            selected = Mathf.Clamp(SessionState.GetInt(TabPref, 0), 0, tabs.Length - 1);
            version = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Addr).Assembly)?.version ?? "dev";
        }

        void OnGUI()
        {
            DrawTopBar();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            tabs[selected].Draw(this);
            GUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        void DrawTopBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("AddrKit", EditorStyles.boldLabel);
                GUILayout.Label("v" + version, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Docs", EditorStyles.toolbarButton, GUILayout.Width(46))) AddrGUI.OpenDocs();
            }

            int chosen = GUILayout.Toolbar(selected, titles, GUILayout.Height(26));
            if (chosen != selected) SelectTab(chosen);
            GUILayout.Space(2);
        }

        // The Debug tab shows live data, so refresh it a few times a second while playing.
        void OnInspectorUpdate()
        {
            if (selected == tabs.Length - 1 && Application.isPlaying) Repaint();
        }

        public void SelectTab(int index)
        {
            selected = index;
            scroll = Vector2.zero;
            SessionState.SetInt(TabPref, index);
            Repaint();
        }
    }
}
