using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AddrKit;
using UnityEngine;

/// <summary>
/// Hands-on tour of AddrKit. Every button is one or two lines of AddrKit code.
/// Press F9 in Play mode to watch the live-load overlay while you click.
/// </summary>
public class AddrKitDemo : MonoBehaviour
{
    // The demo content is created by Tools > AddrKit > Set Up Demo with these addresses and labels.
    // In your own game, use the generated Keys class instead of strings.
    const string Goblin = "AddrKitDemo/Goblin";
    const string Orc = "AddrKitDemo/Orc";
    const string Crate = "AddrKitDemo/Crate";
    const string Barrel = "AddrKitDemo/Barrel";
    const string Level1 = "AddrKitDemo/Level1";
    const string EnemyLabel = "demo-enemy";

    readonly List<GameObject> spawned = new List<GameObject>();
    readonly List<string> log = new List<string>();
    Vector2 logScroll;

    AddrScope levelScope;          // owner for "everything in this level"
    float preloadProgress = -1f;
    int spawnCount;
    bool contentReady = true;

    async void Start()
    {
        await Addr.Initialize();
        contentReady = Addr.Exists(Goblin);
    }

    void Log(string text)
    {
        log.Add($"[{Time.time:0.0}s] {text}");
        if (log.Count > 50) log.RemoveAt(0);
        logScroll.y = float.MaxValue;
    }

    Vector3 NextPosition() => new Vector3((spawnCount++ % 9 - 4) * 1.4f, 0.5f, UnityEngine.Random.Range(0f, 3f));

    // 1. Spawn with `this` as owner: the instance is released when this component is destroyed.
    async Task SpawnGoblin()
    {
        var go = await Addr.Spawn(Goblin, NextPosition(), Quaternion.identity, owner: this);
        if (go != null) spawned.Add(go);
        Log("Spawned Goblin (owner: this)");
    }

    // 2. Despawn one instance by hand.
    void DespawnLast()
    {
        if (spawned.Count == 0) return;
        Addr.Despawn(spawned[spawned.Count - 1]);
        spawned.RemoveAt(spawned.Count - 1);
        Log("Despawned last");
    }

    // 3. Spawn everything with a label in one call. Each instance is tracked by the owner.
    async Task SpawnAllEnemies()
    {
        var enemies = await Addr.SpawnAll(EnemyLabel, owner: this);
        spawned.AddRange(enemies);
        Log($"SpawnAll('{EnemyLabel}') spawned {enemies.Count}");
    }

    // 4. A scope groups loads that belong together, then releases them all with one Dispose.
    async Task StartLevelScope()
    {
        levelScope = levelScope ?? new AddrScope("Level");
        spawned.Add(await Addr.Spawn(Crate, NextPosition(), Quaternion.identity, owner: levelScope));
        spawned.Add(await Addr.Spawn(Barrel, NextPosition(), Quaternion.identity, owner: levelScope));
        Log("Spawned Crate + Barrel in scope 'Level'");
    }

    void EndLevelScope()
    {
        if (levelScope != null) levelScope.Dispose();
        levelScope = null;
        spawned.RemoveAll(go => go == null);
        Log("Disposed scope: everything in it released");
    }

    // 5. Temporary owner: destroying the owner releases what it loaded. No Release call anywhere.
    async Task TemporaryOwnerDemo()
    {
        var temp = new GameObject("Temporary Owner");
        await Addr.Spawn(Orc, NextPosition(), Quaternion.identity, owner: temp);
        Log("Orc spawned with a temporary owner. Destroying owner in 2s...");
        Destroy(temp, 2f);
    }

    // 6. Scenes.
    async Task LoadLevel()
    {
        await Addr.LoadScene(Level1, additive: true);
        Log("Level1 loaded");
    }

    async Task UnloadLevel()
    {
        await Addr.UnloadScene(Level1);
        Log("Level1 unloaded");
    }

    // 7. Preload with a progress bar (downloads to the disk cache; local content is instant).
    async Task PreloadEnemies()
    {
        preloadProgress = 0f;
        await Addr.Preload(EnemyLabel, p => preloadProgress = p);
        Log("Preload finished");
    }

    // 8. A typo gives a helpful message.
    async Task TryTypo()
    {
        await Addr.Spawn("AddrKitDemo/Gobln", owner: this);
    }

    // 9. A load with no owner is global: it stays until released by hand. Watch it in the F9 overlay.
    async Task LeakGlobal()
    {
        await Addr.Load<GameObject>(Orc);
        Log("Loaded Orc with NO owner: it will stay in the overlay until released");
    }

    void ReleaseGlobals()
    {
        Addr.ReleaseAll(null);
        Log("Released all global loads");
    }

    // Buttons call this: errors are logged here instead of disappearing inside an async void.
    async void Run(Func<Task> action)
    {
        try { await action(); }
        catch (AddrException e) { Log(e.Message.Replace("\n", " ")); }
        catch (Exception e) { Debug.LogException(e); }
    }

    // ---------------------------------------------------------------- UI

    void OnGUI()
    {
        float s = Mathf.Max(1f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        GUILayout.BeginArea(new Rect(10, 10, 360, Screen.height / s - 20), GUI.skin.box);
        GUILayout.Label("<b>AddrKit Demo</b>   (F9: live loads)", new GUIStyle(GUI.skin.label) { richText = true });

        if (!contentReady)
        {
            GUILayout.Label("Demo content not found.\nRun  Tools > AddrKit > Set Up Demo  and press Play again.");
            GUILayout.EndArea();
            return;
        }

        Row("Spawn Goblin", () => Run(SpawnGoblin), "Despawn last", DespawnLast);
        Row("SpawnAll 'enemy'", () => Run(SpawnAllEnemies), "Typo demo", () => Run(TryTypo));
        Row("Scope: spawn props", () => Run(StartLevelScope), "Scope: dispose", EndLevelScope);
        Row("Temp owner (auto)", () => Run(TemporaryOwnerDemo), "Preload 'enemy'", () => Run(PreloadEnemies));
        Row("Load Level1", () => Run(LoadLevel), "Unload Level1", () => Run(UnloadLevel));
        Row("Leak: global load", () => Run(LeakGlobal), "Release globals", ReleaseGlobals);

        if (preloadProgress >= 0f)
        {
            var bar = GUILayoutUtility.GetRect(300, 14);
            GUI.Box(bar, GUIContent.none);
            GUI.Box(new Rect(bar.x, bar.y, bar.width * preloadProgress, bar.height), GUIContent.none);
        }

        GUILayout.Space(6);
        logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.ExpandHeight(true));
        foreach (var line in log) GUILayout.Label(line);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    static void Row(string a, Action actionA, string b, Action actionB)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(a)) actionA();
        if (GUILayout.Button(b)) actionB();
        GUILayout.EndHorizontal();
    }
}
