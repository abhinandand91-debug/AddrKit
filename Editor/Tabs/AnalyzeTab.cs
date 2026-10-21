using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Runs <see cref="AddrAnalyzer"/> and lists what it found. Click a row to select the asset.</summary>
    internal sealed class AnalyzeTab : IAddrTab
    {
        List<AnalyzerFinding> findings;

        public string Title => "Analyze";

        public void Draw(AddrKitWindow window)
        {
            var addr = AddrEditorUtil.FindAddrSettings();
            var settings = AddrEditorUtil.AddressablesSettings;
            if (addr == null || settings == null)
            {
                EditorGUILayout.HelpBox("Run Setup first.", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox("Finds assets copied into several bundles, duplicate addresses and oversized assets.", MessageType.Info);
            if (GUILayout.Button("Run Analysis", GUILayout.Height(26)))
                findings = AddrAnalyzer.Run(settings, addr);

            if (findings == null) return;
            EditorGUILayout.Space();

            foreach (AnalyzerFinding finding in findings)
            {
                string icon = finding.Severity == FindingSeverity.Error ? "console.erroricon.sml"
                            : finding.Severity == FindingSeverity.Warning ? "console.warnicon.sml"
                            : "console.infoicon.sml";

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(20));
                    EditorGUILayout.LabelField(finding.Message, EditorStyles.wordWrappedLabel);
                    if (!string.IsNullOrEmpty(finding.AssetPath) && GUILayout.Button("Select", GUILayout.Width(52)))
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(finding.AssetPath);
                }
            }
        }
    }
}
