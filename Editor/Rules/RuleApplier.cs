using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace AddrKit.Editor
{
    public sealed class ApplyReport
    {
        public int Added;
        public int Updated;
        public readonly List<string> Warnings = new List<string>();

        public override string ToString()
            => $"AddrKit rules applied: {Added} added, {Updated} updated, {Warnings.Count} warning(s).";
    }

    /// <summary>Turns rule sets into Addressables groups, addresses and labels.</summary>
    public static class RuleApplier
    {
        static readonly HashSet<string> NeverAddressable = new HashSet<string>
        {
            ".cs", ".dll", ".asmdef", ".asmref", ".meta", ".cginc", ".hlsl", ".md"
        };

        // ---------------------------------------------------------------- pure helpers (unit tested)

        /// <summary>The address a rule gives to an asset.</summary>
        public static string ComputeAddress(AddressMode mode, string prefix, string folderPath, string assetPath)
        {
            string withoutExtension = assetPath.Substring(0, assetPath.Length - Path.GetExtension(assetPath).Length);
            string address;

            switch (mode)
            {
                case AddressMode.FileName:
                    address = Path.GetFileNameWithoutExtension(assetPath);
                    break;
                case AddressMode.FullPath:
                    address = assetPath;
                    break;
                default:
                    string root = (folderPath ?? "").TrimEnd('/') + "/";
                    address = withoutExtension.StartsWith(root, StringComparison.Ordinal)
                        ? withoutExtension.Substring(root.Length)
                        : withoutExtension;
                    break;
            }

            prefix = (prefix ?? "").Trim();
            if (prefix.Length > 0 && !prefix.EndsWith("/")) prefix += "/";
            return prefix + address;
        }

        /// <summary>True when the file type is allowed by the rule's extension list.</summary>
        public static bool ExtensionAllowed(string extensionsCsv, string assetPath)
        {
            string ext = Path.GetExtension(assetPath).ToLowerInvariant();
            if (NeverAddressable.Contains(ext)) return false;
            if (string.IsNullOrWhiteSpace(extensionsCsv)) return true;

            foreach (string part in extensionsCsv.Split(','))
                if (string.Equals("." + part.Trim().TrimStart('.'), ext, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // ---------------------------------------------------------------- finding and applying

        /// <summary>Every asset path the rule currently manages.</summary>
        public static List<string> FindAssets(AddrRule rule)
        {
            var result = new List<string>();
            string folderPath = rule.FolderPath;
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)) return result;

            foreach (string guid in AssetDatabase.FindAssets("", new[] { folderPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (!ExtensionAllowed(rule.extensions, path)) continue;
                if (!rule.includeSubfolders && Path.GetDirectoryName(path)?.Replace('\\', '/') != folderPath) continue;
                result.Add(path);
            }
            return result;
        }

        public static ApplyReport Apply(AddrRuleSet ruleSet, AddressableAssetSettings settings)
        {
            var report = new ApplyReport();
            var addressOwners = new Dictionary<string, string>();

            foreach (AddrRule rule in ruleSet.rules)
            {
                if (!rule.enabled) continue;
                if (rule.folder == null)
                {
                    report.Warnings.Add("A rule has no folder assigned.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(rule.groupName))
                {
                    report.Warnings.Add($"Rule for '{rule.FolderPath}' has no group name.");
                    continue;
                }

                AddressableAssetGroup group = GetOrCreateGroup(settings, rule);
                foreach (string path in FindAssets(rule))
                    ApplyToAsset(settings, rule, group, path, addressOwners, report);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            return report;
        }

        static void ApplyToAsset(AddressableAssetSettings settings, AddrRule rule, AddressableAssetGroup group,
            string path, Dictionary<string, string> addressOwners, ApplyReport report)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            string address = ComputeAddress(rule.addressMode, rule.addressPrefix, rule.FolderPath, path);

            if (addressOwners.TryGetValue(address, out string other) && other != path)
                report.Warnings.Add($"Address '{address}' is used by both '{other}' and '{path}'. Use a different address mode or prefix.");
            addressOwners[address] = path;

            var entry = settings.FindAssetEntry(guid);
            bool isNew = entry == null;
            bool changed = isNew || entry.parentGroup != group || entry.address != address;

            if (entry == null || entry.parentGroup != group)
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;

            foreach (string label in rule.labels)
            {
                if (string.IsNullOrWhiteSpace(label)) continue;
                if (!entry.labels.Contains(label)) changed = true;
                entry.SetLabel(label, true, true, false);
            }

            if (isNew) report.Added++;
            else if (changed) report.Updated++;
        }

        // ---------------------------------------------------------------- groups

        public static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, AddrRule rule)
        {
            var group = settings.FindGroup(rule.groupName)
                        ?? settings.CreateGroup(rule.groupName, false, false, false, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null) return group;

            schema.BundleMode = rule.packing;
            schema.BuildPath.SetVariableByName(settings,
                rule.remote ? AddressableAssetSettings.kRemoteBuildPath : AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings,
                rule.remote ? AddressableAssetSettings.kRemoteLoadPath : AddressableAssetSettings.kLocalLoadPath);
            return group;
        }
    }
}
