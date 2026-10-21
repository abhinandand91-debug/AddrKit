# Quick Start (10 minutes)

## 1. Set up

`Tools > AddrKit > Run Setup`. This creates:

- Addressables settings (if missing)
- `Assets/AddrKit/Resources/AddrKitSettings.asset` (AddrKit options)
- `Assets/AddrKit/AddrRules.asset` (your folder rules)
- `Assets/AddrKit/Generated/Keys.cs` (generated constants)

## 2. Tell AddrKit where your content is

Open `Tools > AddrKit > Open Window > Rules`.

1. Select a folder in the Project window, for example `Assets/Game/Enemies`.
2. Press **Add rule for the selected folder**.
3. Fill in the rule:
   - **Group name**: `Enemies`
   - **Address prefix**: `Enemies/`
   - **Address mode**: `FileName` (so `Goblin.prefab` becomes `Enemies/Goblin`)
   - **Labels**: `enemy`
4. Press **Apply Rules**. After this, rules re-apply by themselves when files change.

Check the **Keys** tab. You will see `Keys.Enemies.Goblin` and `Labels.Enemy`.

## 3. Use it in code

```csharp
using AddrKit;
using AddrKit.Generated;
using UnityEngine;

public class Arena : MonoBehaviour
{
    async void Start()
    {
        // Spawn one
        GameObject goblin = await Addr.Spawn(Keys.Enemies.Goblin, new Vector3(0, 0, 5), Quaternion.identity, owner: this);

        // Load a whole label
        var allEnemies = await Addr.LoadAll<GameObject>(Labels.Enemy, owner: this);
    }
}
```

When the `Arena` object is destroyed, everything it loaded is released. No other code.

## 4. Group loads with a scope

```csharp
using var level = new AddrScope("Level 1");
await Addr.Spawn(Keys.Props.Crate, pos, rot, owner: level);
await Addr.Spawn(Keys.Props.Barrel, pos, rot, owner: level);
// leaving the block disposes the scope and releases both
```

For a level that lasts across many frames, keep the scope in a field and call `Dispose()` when the level ends.

## 5. No code at all

- Add the **AddrKit/Spawner** component, pick an address from the dropdown, done.
- Add **AddrKit/Scene Loader** to load a scene by address.
- Put `[AddrKey]` on any `string` field to get the dropdown in your own scripts.

## 6. Loading screens

```csharp
await Addr.Preload(Labels.Level1, p => progressBar.value = p);
```

Local content completes instantly. With remote content (see below) this downloads and caches it.

## 7. Watch what is loaded

Press **Play**, then **F9** in the Game view (or use the **Debug** tab in the AddrKit window).
Each row is something currently in memory: key, type, owner, age. The Debug tab also shows
the line of code that loaded it. A row that never disappears is a leak.

## 8. Check your setup

`AddrKit window > Analyze > Run Analysis` finds:

- assets copied into several bundles (fix: make them addressable)
- two assets sharing one address
- very large assets

## 9. Remote content (optional)

1. Tick **remote** on a rule (that group is then downloaded instead of shipped).
2. `Build` tab: **Turn Remote On**, then **Build Content**.
3. **Start Local Server** to host `ServerData/` on your machine.
4. Switch the Play mode script to **Use Existing Build**, press Play.
5. In production, upload `ServerData/<platform>/` to your CDN and set the Server URL to it before building.

Call `await Addr.UpdateCatalogs()` at startup to pick up content published after your build.
