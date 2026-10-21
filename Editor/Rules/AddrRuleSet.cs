using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace AddrKit.Editor
{
    public enum AddressMode
    {
        [Tooltip("Folder-relative path without extension, e.g. 'Goblin/Goblin'.")]
        RelativePath,
        [Tooltip("Only the file name without extension, e.g. 'Goblin'. Names must be unique.")]
        FileName,
        [Tooltip("The full project path, e.g. 'Assets/Game/Enemies/Goblin.prefab'.")]
        FullPath
    }

    /// <summary>
    /// "Everything in this folder goes into this group with these labels."
    /// </summary>
    [Serializable]
    public sealed class AddrRule
    {
        public bool enabled = true;
        [Tooltip("Folder whose assets this rule manages.")]
        public DefaultAsset folder;
        [Tooltip("Addressables group the assets are put into. Created if missing.")]
        public string groupName = "Default";
        [Tooltip("How each asset's address is built.")]
        public AddressMode addressMode = AddressMode.RelativePath;
        [Tooltip("Text put in front of every address, e.g. 'Enemies/'.")]
        public string addressPrefix = "";
        [Tooltip("Labels added to every asset (e.g. 'enemy', 'level1').")]
        public List<string> labels = new List<string>();
        [Tooltip("Comma-separated file extensions to include. Empty = every asset type except scripts.")]
        public string extensions = "prefab,unity,asset,png,jpg,jpeg,wav,mp3,ogg,mp4,json,txt,bytes";
        public bool includeSubfolders = true;
        [Tooltip("Pack Together = one bundle per group. Pack Separately = one bundle per asset (finest control over memory).")]
        public BundledAssetGroupSchema.BundlePackingMode packing = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        [Tooltip("Serve this group from the remote server (downloadable content) instead of shipping it in the build.")]
        public bool remote;

        public string FolderPath => folder != null ? AssetDatabase.GetAssetPath(folder) : null;
    }

    /// <summary>
    /// A list of <see cref="AddrRule"/>. Create with Assets > Create > AddrKit > Rule Set.
    /// Check it into version control so the whole team shares one organization.
    /// </summary>
    [CreateAssetMenu(menuName = "AddrKit/Rule Set", fileName = "AddrRules")]
    public sealed class AddrRuleSet : ScriptableObject
    {
        public List<AddrRule> rules = new List<AddrRule>();
    }
}
