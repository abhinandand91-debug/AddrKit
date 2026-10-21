# AddrKit

A friendly layer on top of Unity Addressables. Load things in one line, never write `Release` by hand,
catch typos at compile time, and see exactly what is in memory.

```csharp
using AddrKit;
using AddrKit.Generated;

var goblin = await Addr.Spawn(Keys.Enemies.Goblin, position, Quaternion.identity, owner: this);
var icon   = await Addr.Load<Sprite>(Keys.Icons.Sword, owner: this);
await Addr.Preload(Labels.Level1, p => bar.value = p);
```

When `this` is destroyed, the goblin and the icon are released. That is the whole memory model.

## Install

Package Manager > Add package from disk (or git URL) > `package.json`.
Requires Unity 6 and Addressables 2.x (installed automatically).

## Set up in 3 steps

1. `Tools > AddrKit > Run Setup`
2. Open `Tools > AddrKit > Open Window > Rules`, select a content folder in the Project window, press **Add rule for the selected folder**.
3. Use the generated `Keys` in code.

Full walkthrough: [Documentation~/QuickStart.md](Documentation~/QuickStart.md).
Concepts explained plainly: [Documentation~/Concepts.md](Documentation~/Concepts.md).
When something goes wrong: [Documentation~/Troubleshooting.md](Documentation~/Troubleshooting.md).

## What is in the box

| Piece | What it gives you |
|---|---|
| `Addr` | One static class: load, spawn, scenes, preload, release |
| Owners and `AddrScope` | Automatic release when the owner goes away |
| `Keys` / `Labels` | Generated constants, so a typo is a compile error |
| Rules | "Everything in this folder goes in this group with these labels" |
| Window | Setup, rules, keys, analyzer, build, remote test server, live debug |
| `[AddrKey]` | Address dropdown in the Inspector |
| `AddrSpawner`, `AddrSceneLoader` | No-code components |
| Overlay (F9) | Live list of loaded assets in the Editor and dev builds |

## API at a glance

| Call | Does |
|---|---|
| `Addr.Load<T>(key, owner)` | Loads one asset |
| `Addr.LoadAll<T>(label, owner)` | Loads every asset with the label |
| `Addr.Spawn(key, pos, rot, owner, parent)` | Instantiates a prefab |
| `Addr.SpawnUnder(key, parent, owner)` | Instantiates as a child |
| `Addr.LoadScene(key, additive)` / `UnloadScene(key)` | Scenes by address |
| `Addr.Despawn(go)` / `Addr.Unload(asset)` | Release one thing early |
| `Addr.ReleaseAll(owner)` | Release everything an owner loaded |
| `Addr.Preload(label, onProgress)` | Download and cache, with progress |
| `Addr.GetDownloadSize(label)` | Bytes left to download |
| `Addr.UpdateCatalogs()` | Pick up new remote content |
| `Addr.Exists(key)` / `Addr.Snapshot()` | Check a key / list live loads |

## Design

Small layers, each in its own folder: `Runtime/Core` (service, ownership, records, errors),
`Runtime/Addr.cs` (the facade), `Runtime/Components`, `Runtime/Diagnostics`, `Editor/Tools` (generator, analyzer, remote),
`Editor/Rules`, `Editor/Tabs` (one file per window tab). `Addr` is a thin wrapper over `IAssetService`;
swap `Addr.Service` in tests or for dependency injection.

## Tests

`Tests/Editor` covers key generation, typo matching and rule logic. `Tests/PlayMode` loads, spawns and releases
real addressable content. Run both from `Window > General > Test Runner`.
