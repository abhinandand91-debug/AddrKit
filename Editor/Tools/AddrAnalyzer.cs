using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace AddrKit.Editor
{
    public enum FindingSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class AnalyzerFinding
    {
        public FindingSeverity Severity;
        public string Message;
        public string AssetPath;
    }

    /// <summary>
    /// Looks for the usual Addressables mistakes: assets duplicated across bundles,
    /// two assets sharing an address, and oversized assets.
    /// </summary>
    public static class AddrAnalyzer
    {
        public static List<AnalyzerFinding> Run(AddressableAssetSettings settings, AddrSettings addr)
        {
            var findings = new List<AnalyzerFinding>();
            var entries = AddrEditorUtil.AllEntries(settings);

            CheckDuplicateAddresses(entries, findings);
            CheckDuplicatedDependencies(settings, entries, findings);
            CheckLargeAssets(entries, addr.largeAssetWarningMB, findings);
            CheckStaleKeys(settings, addr, findings);

            if (findings.Count == 0)
                findings.Add(new AnalyzerFinding { Severity = FindingSeverity.Info, Message = $"No problems found in {entries.Count} addressable asset(s)." });
            return findings;
        }

        /// <summary>The generated Keys file should match the current addresses.</summary>
        static void CheckStaleKeys(AddressableAssetSettings settings, AddrSettings addr, List<AnalyzerFinding> findings)
        {
            bool exists = File.Exists(addr.keysOutputPath);
            if (exists && File.ReadAllText(addr.keysOutputPath) == KeyGenerator.BuildSourceFromProject(settings, addr)) return;

            findings.Add(new AnalyzerFinding
            {
                Severity = FindingSeverity.Warning,
                Message = exists
                    ? "Generated Keys are out of date. Run Tools > AddrKit > Generate Keys."
                    : "No generated Keys file yet. Run Tools > AddrKit > Generate Keys.",
                AssetPath = exists ? addr.keysOutputPath : null
            });
        }

        static void CheckDuplicateAddresses(List<AddressableAssetEntry> entries, List<AnalyzerFinding> findings)
        {
            foreach (var group in entries.GroupBy(e => e.address).Where(g => g.Count() > 1))
            {
                findings.Add(new AnalyzerFinding
                {
                    Severity = FindingSeverity.Error,
                    Message = $"Address '{group.Key}' is used by {group.Count()} assets. Loads become ambiguous.",
                    AssetPath = group.First().AssetPath
                });
            }
        }

        /// <summary>
        /// An asset that is not addressable but is referenced from two different bundles is copied into both.
        /// Making it addressable (in one place) stores it once.
        /// </summary>
        static void CheckDuplicatedDependencies(AddressableAssetSettings settings, List<AddressableAssetEntry> entries, List<AnalyzerFinding> findings)
        {
            var bundlesUsing = new Dictionary<string, HashSet<string>>();

            foreach (var entry in entries)
            {
                // Pack Separately: every asset is its own bundle. Otherwise the whole group is one.
                var schema = entry.parentGroup.GetSchema<BundledAssetGroupSchema>();
                bool separate = schema != null && schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                string bundleId = separate ? entry.AssetPath : entry.parentGroup.Name;

                foreach (string dependency in AssetDatabase.GetDependencies(entry.AssetPath, true))
                {
                    if (dependency == entry.AssetPath || !dependency.StartsWith("Assets/")) continue;
                    if (dependency.EndsWith(".cs")) continue;
                    if (settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(dependency)) != null) continue;

                    if (!bundlesUsing.TryGetValue(dependency, out var bundles))
                        bundlesUsing[dependency] = bundles = new HashSet<string>();
                    bundles.Add(bundleId);
                }
            }

            foreach (var pair in bundlesUsing.Where(p => p.Value.Count > 1).OrderBy(p => p.Key))
            {
                findings.Add(new AnalyzerFinding
                {
                    Severity = FindingSeverity.Warning,
                    Message = $"'{Path.GetFileName(pair.Key)}' is copied into {pair.Value.Count} bundles. Mark it Addressable to store it once.",
                    AssetPath = pair.Key
                });
            }
        }

        static void CheckLargeAssets(List<AddressableAssetEntry> entries, int limitMB, List<AnalyzerFinding> findings)
        {
            long limit = limitMB * 1024L * 1024L;
            foreach (var entry in entries)
            {
                if (!File.Exists(entry.AssetPath)) continue;
                long size = new FileInfo(entry.AssetPath).Length;
                if (size > limit)
                {
                    findings.Add(new AnalyzerFinding
                    {
                        Severity = FindingSeverity.Warning,
                        Message = $"'{entry.address}' is {size / (1024f * 1024f):0.#} MB (limit {limitMB} MB). Consider compressing or splitting it.",
                        AssetPath = entry.AssetPath
                    });
                }
            }
        }
    }
}
