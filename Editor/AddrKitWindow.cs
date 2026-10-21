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
        IAddrTab[] tabs;
        string[] titles;
        int selected;
        Vector2 scroll;

        public static void Open()
        {
            var window = GetWindow<AddrKitWindow>("AddrKit");
            window.minSize = new Vector2(420, 360);
        }

        void OnEnable()
        {
            tabs = new IAddrTab[]
            {
                new SetupTab(), new RulesTab(), new KeysTab(), new AnalyzeTab(), new BuildTab(), new DebugTab()
            };
            titles = System.Array.ConvertAll(tabs, t => t.Title);
        }

        void OnGUI()
        {
            selected = GUILayout.Toolbar(selected, titles, GUILayout.Height(24));
            EditorGUILayout.Space(4);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            tabs[selected].Draw(this);
            EditorGUILayout.EndScrollView();
        }

        // The Debug tab shows live data, so refresh it a few times a second while playing.
        void OnInspectorUpdate()
        {
            if (selected == tabs.Length - 1 && Application.isPlaying) Repaint();
        }

        public void SelectTab(int index) => selected = index;
    }
}
