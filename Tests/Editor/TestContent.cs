using AddrKit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.TestTools;

namespace AddrKit.Tests
{
    /// <summary>Creates two addressable prefabs before the play mode tests run, and removes them afterwards.</summary>
    public class TestContent : IPrebuildSetup, IPostBuildCleanup
    {
        const string Folder = "Assets/_AddrKitTests";
        const string GroupName = "AddrKitTests";

        public void Setup()
        {
            KeyGenerator.Suspended = true;   // keep the test addresses out of the project's Keys file
            var settings = AddrEditorUtil.GetOrCreateAddressablesSettings();
            AddrEditorUtil.EnsureFolder(Folder);

            var group = settings.FindGroup(GroupName) ?? settings.CreateGroup(GroupName, false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            Add(settings, group, "Cube", PrimitiveType.Cube, TestKeys.Cube);
            Add(settings, group, "Sphere", PrimitiveType.Sphere, TestKeys.Sphere);
            AssetDatabase.SaveAssets();
        }

        public void Cleanup()
        {
            KeyGenerator.Suspended = true;
            var settings = AddrEditorUtil.AddressablesSettings;
            var group = settings?.FindGroup(GroupName);
            if (group != null) settings.RemoveGroup(group);
            settings?.RemoveLabel(TestKeys.Label);   // labels outlive their entries, so remove it explicitly
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.SaveAssets();

            KeyGenerator.Suspended = false;
            KeyGenerator.Generate();         // restores the Keys file if it changed
        }

        static void Add(AddressableAssetSettings settings, AddressableAssetGroup group, string name, PrimitiveType type, string address)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            string path = $"{Folder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = address;
            entry.SetLabel(TestKeys.Label, true, true);
        }
    }
}
