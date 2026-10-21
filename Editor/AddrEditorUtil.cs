using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Small helpers shared by the AddrKit editor tools.</summary>
    public static class AddrEditorUtil
    {
        public const string DefaultRoot = "Assets/AddrKit";

        /// <summary>The project's Addressables settings, or null when Addressables is not set up yet.</summary>
        public static AddressableAssetSettings AddressablesSettings => AddressableAssetSettingsDefaultObject.Settings;

        public static AddressableAssetSettings GetOrCreateAddressablesSettings()
            => AddressableAssetSettingsDefaultObject.GetSettings(true);

        public static T FindFirst<T>() where T : Object
        {
            var all = FindAll<T>();
            return all.Count > 0 ? all[0] : null;
        }

        public static List<T> FindAll<T>() where T : Object
        {
            var result = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) result.Add(asset);
            }
            return result;
        }

        public static AddrSettings FindAddrSettings() => FindFirst<AddrSettings>();

        public static AddrSettings GetOrCreateAddrSettings()
        {
            var existing = FindAddrSettings();
            if (existing != null) return existing;

            EnsureFolder(DefaultRoot + "/Resources");
            var created = ScriptableObject.CreateInstance<AddrSettings>();
            AssetDatabase.CreateAsset(created, $"{DefaultRoot}/Resources/{AddrSettings.ResourceName}.asset");
            AssetDatabase.SaveAssets();
            return created;
        }

        /// <summary>Creates every missing folder in an "Assets/..." path.</summary>
        public static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
        }

        /// <summary>All top-level asset entries (folders expanded) across every group.</summary>
        public static List<AddressableAssetEntry> AllEntries(AddressableAssetSettings settings)
        {
            var result = new List<AddressableAssetEntry>();
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                foreach (var entry in group.entries)
                {
                    if (AssetDatabase.IsValidFolder(entry.AssetPath)) entry.GatherAllAssets(result, false, true, false);
                    else result.Add(entry);
                }
            }
            return result;
        }
    }
}
