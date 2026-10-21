using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Runs <see cref="AddrAnalyzer"/>. Each finding says what is wrong and, where possible, fixes it in one click.</summary>
    internal sealed class AnalyzeTab : IAddrTab
    {
        List<AnalyzerFinding> findings;
        bool ranOnce;

        public string Title => "Analyze";

        public void Draw(AddrKitWindow window)
        {
            AddrGUI.Header("Analyze", "Finds the usual Addressables mistakes: assets copied into several bundles, duplicate addresses, stale keys and oversized files.");

            var addr = AddrEditorUtil.FindAddrSettings();
            var settings = AddrEditorUtil.AddressablesSettings;
            if (addr == null || settings == null)
            {
                using (new AddrGUI.CardScope("Not set up yet"))
                    if (AddrGUI.PrimaryButton("Set Up AddrKit")) AddrKitSetup.RunSetup();
                return;
            }

            // Analyze once automatically the first time the page is shown.
            if (!ranOnce) { findings = AddrAnalyzer.Run(settings, addr); ranOnce = true; }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                int errors = findings.Count(f => f.Severity == FindingSeverity.Error);
                int warnings = findings.Count(f => f.Severity == FindingSeverity.Warning);
                if (errors == 0 && warnings == 0) AddrGUI.DrawPill("All clear", Tone.Good);
                if (errors > 0) AddrGUI.DrawPill($"{errors} error{(errors == 1 ? "" : "s")}", Tone.Bad);
                if (warnings > 0) AddrGUI.DrawPill($"{warnings} warning{(warnings == 1 ? "" : "s")}", Tone.Warn);
                GUILayout.FlexibleSpace();
                if (AddrGUI.Button("Run again", 22, GUILayout.Width(80))) findings = AddrAnalyzer.Run(settings, addr);
            }
            GUILayout.Space(4);

            foreach (AnalyzerFinding finding in findings.OrderByDescending(f => f.Severity).ToList())
                DrawFinding(finding, settings, addr);
        }

        void DrawFinding(AnalyzerFinding finding, UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings, AddrSettings addr)
        {
            Tone tone = finding.Severity == FindingSeverity.Error ? Tone.Bad : finding.Severity == FindingSeverity.Warning ? Tone.Warn : Tone.Info;
            string label = finding.Severity == FindingSeverity.Error ? "Error" : finding.Severity == FindingSeverity.Warning ? "Warning" : "Info";

            using (new AddrGUI.CardScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    AddrGUI.DrawPill(label, tone);
                    GUILayout.Label(finding.Message, EditorStyles.wordWrappedLabel);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (!string.IsNullOrEmpty(finding.AssetPath))
                        GUILayout.Label(finding.AssetPath, AddrGUI.Hint);
                    GUILayout.FlexibleSpace();
                    if (!string.IsNullOrEmpty(finding.AssetPath) && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(finding.AssetPath);
                    if (finding.Fix != null && GUILayout.Button(finding.FixLabel, EditorStyles.miniButton, GUILayout.Width(130)))
                    {
                        finding.Fix();
                        findings = AddrAnalyzer.Run(settings, addr);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
    }
}
