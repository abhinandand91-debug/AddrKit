using System.IO;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>
    /// First page. Before setup: one big button. After setup: a small dashboard and the next things to do.
    /// </summary>
    internal sealed class SetupTab : IAddrTab
    {
        public string Title => "Setup";

        public void Draw(AddrKitWindow window)
        {
            var addressables = AddrEditorUtil.AddressablesSettings;
            var addr = AddrEditorUtil.FindAddrSettings();
            var ruleSets = AddrEditorUtil.FindAll<AddrRuleSet>();
            bool keysExist = addr != null && File.Exists(addr.keysOutputPath);
            bool ready = addressables != null && addr != null && ruleSets.Count > 0 && keysExist;

            if (!ready) DrawWelcome(addressables != null, addr != null, ruleSets.Count > 0, keysExist);
            else DrawDashboard(window, addressables, addr, ruleSets);
        }

        // ---------------------------------------------------------------- not set up yet

        static void DrawWelcome(bool hasAddressables, bool hasSettings, bool hasRules, bool hasKeys)
        {
            AddrGUI.Header("Welcome to AddrKit", "Load assets in one line, release them automatically, and see what is in memory.");

            using (new AddrGUI.CardScope("Set up your project"))
            {
                GUILayout.Label("One click creates everything AddrKit needs. It changes nothing else in your project.", AddrGUI.Hint);
                GUILayout.Space(4);
                Row("Addressables settings", hasAddressables);
                Row("AddrKit settings", hasSettings);
                Row("Rule set (folders to addresses)", hasRules);
                Row("Generated Keys class", hasKeys);
                GUILayout.Space(8);
                if (AddrGUI.PrimaryButton("Set Up AddrKit", 36)) AddrKitSetup.RunSetup();
            }

            using (new AddrGUI.CardScope("What you will be able to write"))
            {
                GUILayout.Label("var goblin = await Addr.Spawn(Keys.Enemies.Goblin, owner: this);", AddrGUI.Mono);
                GUILayout.Label("When 'this' is destroyed, the goblin is released. No Release calls.", AddrGUI.Hint);
            }
        }

        static void Row(string label, bool done)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                AddrGUI.DrawPill(done ? "Ready" : "To do", done ? Tone.Good : Tone.Neutral);
                GUILayout.Label(label);
            }
        }

        // ---------------------------------------------------------------- set up

        static void DrawDashboard(AddrKitWindow window, UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings,
            AddrSettings addr, System.Collections.Generic.List<AddrRuleSet> ruleSets)
        {
            AddrGUI.Header("Overview", "Your project is set up. Here is where things stand.");

            int ruleCount = 0;
            foreach (var set in ruleSets) ruleCount += set.rules.Count;
            int assets = AddrEditorUtil.AllEntries(settings).Count;

            using (new AddrGUI.CardScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    AddrGUI.Stat("groups", settings.groups.Count);
                    GUILayout.Space(14);
                    AddrGUI.Stat("assets", assets);
                    GUILayout.Space(14);
                    AddrGUI.Stat("labels", Mathf.Max(0, new System.Collections.Generic.List<string>(settings.GetLabels()).FindAll(l => l != "default").Count));
                    GUILayout.Space(14);
                    AddrGUI.Stat("rules", ruleCount);
                    GUILayout.FlexibleSpace();
                }
                AddrGUI.Separator();
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool upToDate = File.Exists(addr.keysOutputPath) &&
                                    File.ReadAllText(addr.keysOutputPath) == KeyGenerator.BuildSourceFromProject(settings, addr);
                    AddrGUI.DrawPill(upToDate ? "Keys up to date" : "Keys out of date", upToDate ? Tone.Good : Tone.Warn);
                    AddrGUI.DrawPill(addr.autoApplyRules ? "Rules auto-apply" : "Rules manual", Tone.Neutral);
                    GUILayout.FlexibleSpace();
                    if (!upToDate && GUILayout.Button("Regenerate", EditorStyles.miniButton)) KeyGenerator.Generate();
                }
            }

            if (ruleCount == 0)
            {
                using (new AddrGUI.CardScope("Next: tell AddrKit where your content is"))
                {
                    GUILayout.Label("You have no rules yet, so nothing is addressable. Select a content folder in the Project window and add a rule for it.", AddrGUI.Hint);
                    if (AddrGUI.PrimaryButton("Open Rules")) window.SelectTab(1);
                }
            }
            else
            {
                using (new AddrGUI.CardScope("Next steps"))
                {
                    Step(window, "Review your rules", "Rules", 1);
                    Step(window, "Check for duplicated assets and oversized files", "Analyze", 3);
                    Step(window, "Build content or test remote downloads", "Build", 4);
                    Step(window, "Press Play, then F9, to watch live loads", "Debug", 5);
                }
            }

            using (new AddrGUI.CardScope("Learn"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (AddrGUI.Button("Documentation", 24)) AddrGUI.OpenDocs();
                    if (AddrGUI.Button("Import the Demo sample", 24))
                        EditorApplication.ExecuteMenuItem("Window/Package Manager");
                }
                GUILayout.Label("The Demo sample (Package Manager > AddrKit > Samples) is a playable tour of every feature.", AddrGUI.Hint);
            }
        }

        static void Step(AddrKitWindow window, string text, string go, int tab)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(text);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(go, EditorStyles.miniButton, GUILayout.Width(64))) window.SelectTab(tab);
            }
        }
    }
}
