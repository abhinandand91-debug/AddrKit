using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>The Tools > AddrKit menu and the one-click setup behind it.</summary>
    public static class AddrKitSetup
    {
        public const string DefaultRulesPath = AddrEditorUtil.DefaultRoot + "/AddrRules.asset";

        [MenuItem("Tools/AddrKit/Open Window", priority = 0)]
        public static void OpenWindow() => AddrKitWindow.Open();

        /// <summary>Creates Addressables settings, AddrKit settings and a rule set; applies rules; writes keys.</summary>
        [MenuItem("Tools/AddrKit/Run Setup", priority = 10)]
        public static void RunSetup()
        {
            AddrEditorUtil.GetOrCreateAddressablesSettings();
            AddrEditorUtil.GetOrCreateAddrSettings();

            if (AddrEditorUtil.FindFirst<AddrRuleSet>() == null)
            {
                AddrEditorUtil.EnsureFolder(AddrEditorUtil.DefaultRoot);
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AddrRuleSet>(), DefaultRulesPath);
            }

            ApplyRules(quiet: true);
            KeyGenerator.Generate();
            AssetDatabase.SaveAssets();
            Debug.Log("[AddrKit] Setup complete. Add a rule in the AddrKit window (Rules tab) to organize a folder.");
        }

        [MenuItem("Tools/AddrKit/Apply Rules", priority = 11)]
        public static void ApplyRulesMenu() => ApplyRules(quiet: false);

        public static void ApplyRules(bool quiet = false)
        {
            var settings = AddrEditorUtil.AddressablesSettings;
            if (settings == null)
            {
                if (!quiet) Debug.LogWarning("[AddrKit] Addressables is not set up. Run Tools > AddrKit > Run Setup.");
                return;
            }

            foreach (var ruleSet in AddrEditorUtil.FindAll<AddrRuleSet>())
            {
                ApplyReport report = RuleApplier.Apply(ruleSet, settings);
                foreach (string warning in report.Warnings) Debug.LogWarning("[AddrKit] " + warning, ruleSet);
                if (!quiet || report.Added + report.Updated > 0) Debug.Log("[AddrKit] " + report, ruleSet);
            }

            // New addresses should show up in Keys right away.
            var addr = AddrEditorUtil.FindAddrSettings();
            if (addr != null && addr.autoGenerateKeys) KeyGenerator.Generate();
        }

        [MenuItem("Tools/AddrKit/Generate Keys", priority = 12)]
        public static void GenerateKeys()
        {
            bool written = KeyGenerator.Generate();
            var addr = AddrEditorUtil.FindAddrSettings();
            Debug.Log(written ? $"[AddrKit] Keys written to {addr?.keysOutputPath}" : "[AddrKit] Keys are already up to date.");
        }

        [MenuItem("Tools/AddrKit/Build Content", priority = 20)]
        public static void BuildContent()
        {
            var settings = AddrEditorUtil.AddressablesSettings;
            if (settings == null) { Debug.LogWarning("[AddrKit] Addressables is not set up."); return; }

            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var result);
            if (string.IsNullOrEmpty(result.Error)) Debug.Log($"[AddrKit] Content built in {result.Duration:0.0}s → {result.OutputPath}");
            else Debug.LogError("[AddrKit] Content build failed: " + result.Error);
        }
    }
}
