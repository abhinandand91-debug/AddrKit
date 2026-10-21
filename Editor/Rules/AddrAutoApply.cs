using System.Linq;
using UnityEditor;

namespace AddrKit.Editor
{
    /// <summary>Re-applies rules when assets inside a rule folder are added, moved or changed.</summary>
    internal sealed class AddrAutoApply : AssetPostprocessor
    {
        static bool pending;

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (pending || EditorApplication.isPlayingOrWillChangePlaymode) return;

            var settings = AddrEditorUtil.FindAddrSettings();
            if (settings == null || !settings.autoApplyRules) return;

            var folders = AddrEditorUtil.FindAll<AddrRuleSet>()
                .SelectMany(set => set.rules)
                .Where(rule => rule.enabled && rule.folder != null)
                .Select(rule => rule.FolderPath + "/")
                .ToList();
            if (folders.Count == 0) return;

            bool touchesRuleFolder = imported.Concat(moved).Any(path => folders.Any(path.StartsWith));
            if (!touchesRuleFolder) return;

            pending = true;
            EditorApplication.delayCall += () =>
            {
                pending = false;
                AddrKitSetup.ApplyRules(quiet: true);
            };
        }
    }
}
