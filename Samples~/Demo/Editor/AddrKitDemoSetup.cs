using System.Collections.Generic;
using System.IO;
using AddrKit.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AddrKit.Samples
{
    /// <summary>
    /// Tools > AddrKit > Set Up Demo. Generates demo content (primitive prefabs, two scenes), registers it with
    /// AddrKit rules, and builds the demo scene. Everything is created with the project's own render pipeline,
    /// so there are no pipeline-specific assets to break.
    /// </summary>
    public static class AddrKitDemoSetup
    {
        const string Root = "Assets/AddrKitDemoContent";

        [MenuItem("Tools/AddrKit/Set Up Demo", priority = 100)]
        public static void Run()
        {
            AddrKitSetup.RunSetup();
            foreach (string dir in new[] { "Enemies", "Props", "Levels", "Materials" })
                AddrEditorUtil.EnsureFolder($"{Root}/{dir}");

            Material shared = MakeMaterial("Shared", Color.gray);
            MakePrefab("Enemies/Goblin", PrimitiveType.Capsule, MakeMaterial("Goblin", Color.green));
            MakePrefab("Enemies/Orc", PrimitiveType.Capsule, MakeMaterial("Orc", Color.red));
            MakePrefab("Enemies/Troll", PrimitiveType.Capsule, MakeMaterial("Troll", Color.blue));
            MakePrefab("Props/Crate", PrimitiveType.Cube, shared);
            MakePrefab("Props/Barrel", PrimitiveType.Cylinder, shared);
            MakeLevel("Level1", new Color(0.2f, 0.4f, 0.2f));
            AssetDatabase.SaveAssets();

            RegisterRules();
            AddrKitSetup.ApplyRules(quiet: true);
            KeyGenerator.Generate();

            string scenePath = MakeDemoScene();
            AssetDatabase.Refresh();
            Debug.Log("[AddrKit] Demo ready. Press Play, then F9 to watch live loads.");
            EditorSceneManager.OpenScene(scenePath);
        }

        static void RegisterRules()
        {
            const string rulesPath = Root + "/AddrKitDemoRules.asset";
            var set = AssetDatabase.LoadAssetAtPath<AddrRuleSet>(rulesPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<AddrRuleSet>();
                AssetDatabase.CreateAsset(set, rulesPath);
            }

            set.rules = new List<AddrRule>
            {
                Rule("Enemies", "AddrKitDemo-Enemies", BundledAssetGroupSchema.BundlePackingMode.PackTogether, "demo-enemy"),
                Rule("Props", "AddrKitDemo-Props", BundledAssetGroupSchema.BundlePackingMode.PackSeparately),
                Rule("Levels", "AddrKitDemo-Levels", BundledAssetGroupSchema.BundlePackingMode.PackTogether, "demo-level"),
            };
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        static AddrRule Rule(string folder, string group, BundledAssetGroupSchema.BundlePackingMode packing, params string[] labels)
        {
            return new AddrRule
            {
                folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>($"{Root}/{folder}"),
                groupName = group,
                addressPrefix = "AddrKitDemo/",
                addressMode = AddressMode.FileName,
                packing = packing,
                labels = new List<string>(labels)
            };
        }

        static string MakeDemoScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2, 1, 2);
            ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Ground", new Color(0.22f, 0.27f, 0.33f));

            var cam = Camera.main;
            cam.transform.position = new Vector3(0, 4.5f, -8.5f);
            cam.transform.LookAt(new Vector3(0, 0.5f, 1.5f));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f);

            new GameObject("Demo").AddComponent<AddrKitDemo>();

            // No-code spawning with the AddrKey dropdown.
            var spawner = new GameObject("Crate Spawner").AddComponent<AddrKit.AddrSpawner>();
            spawner.key = "AddrKitDemo/Crate";
            spawner.transform.position = new Vector3(5, 0.5f, 0);

            const string path = Root + "/AddrKitDemo.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static Material MakeMaterial(string name, Color color)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("HDRP/Lit")
                         ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void MakePrefab(string relativePath, PrimitiveType type, Material material)
        {
            string path = $"{Root}/{relativePath}.prefab";
            if (File.Exists(path)) return;

            var go = GameObject.CreatePrimitive(type);
            go.name = Path.GetFileName(relativePath);
            go.GetComponent<Renderer>().sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void MakeLevel(string name, Color color)
        {
            string path = $"{Root}/Levels/{name}.unity";
            if (File.Exists(path)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name + "_Floor";
            floor.transform.position = new Vector3(0, -0.6f, 8);
            floor.transform.localScale = new Vector3(8, 0.2f, 4);
            floor.GetComponent<Renderer>().sharedMaterial = MakeMaterial(name + "Floor", color);
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
