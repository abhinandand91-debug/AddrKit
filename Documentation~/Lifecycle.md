# Lifecycle and ownership contract

This page is the precise version of "what does AddrKit guarantee". Everything here is covered by the PlayMode tests.

## Who owns what

| Thing | Owned by | Released when |
|---|---|---|
| An asset from `Load` / `LoadAll` | The owner you passed (or Global if none) | The owner is destroyed or disposed, or you call `Unload(asset)` / `ReleaseAll(owner)` |
| An instance from `Spawn` / `SpawnAll` / `SpawnUnder` | The owner | Same, or `Despawn(instance)`. The instance is **destroyed** and its memory released together |
| A scene from `LoadScene` | Global by default | `UnloadScene(key)` (or pass an owner to tie it to one) |
| Downloaded bundles from `Preload` | The disk cache. No AddrKit handle | Never by AddrKit: it is a cache, not memory |

AddrKit never makes you hold a handle. The only things you hold are the loaded asset or the instance.

## What `owner: this` guarantees

- If `this` is a `MonoBehaviour` or `GameObject`, everything loaded with it is released when its GameObject is destroyed.
- If `this` is an `AddrScope`, everything is released on `Dispose()`.
- Releases happen once. Disposing a scope twice is safe.
- Releasing one owner never frees an asset another owner still uses (Addressables reference-counts the underlying bundle).

## When the owner goes away during a load

The load is abandoned and cleaned up. The task **completes with `null`** (an empty list for `LoadAll` / `SpawnAll`),
**does not throw**, and nothing is left in memory or in the scene. So after `await Addr.Spawn(..., owner: this)`,
check for `null` if `this` might have been destroyed in the meantime.

## Cancellation

Every load takes an optional `CancellationToken`. Cancelling abandons the load, cleans up like above,
and the task ends **cancelled** (`OperationCanceledException`). A token that is already cancelled throws before starting.

## Errors

A failed load (bad address, missing bundle, network failure) throws `AddrException` with the key and, for typos,
a "Did you mean" list. A failed load is never left tracked.

## Several callers, same asset

Two owners can load the same address. Addressables keeps the asset until both release it. In the Debug tab you
see one row per owner.

## What preload means

`Addr.Preload(label)` **downloads bundles to the disk cache**. It does not load assets into memory and keeps no handle.
Progress is download progress (0 to 1). Content that is local or already cached completes immediately
(on WebGL, "local" bundles are still fetched over HTTP, so Preload and `GetDownloadSize` reflect that).
To keep assets in memory, use `Load` / `LoadAll` / `SpawnAll` with an owner.

## `LoadAll` vs `SpawnAll`

- `LoadAll<T>(label, owner)` returns the loaded assets (prefab assets, sprites...). One handle covers them all.
  If you `Instantiate` them yourself, those instances are **not** tracked: you must destroy them, and the owner must
  outlive them (the prefabs' dependencies stay loaded while the owner lives).
- `SpawnAll(label, owner)` instantiates every prefab through AddrKit; each instance is tracked and destroyed with the owner.
  Prefer it for prefabs.

## Scenes

Scenes are global unless you pass an owner. Unload with `Addr.UnloadScene(key)`; the scene's bundle is released with it.
Instances spawned with an owner that lives in the unloaded scene are released when that owner's GameObject is destroyed
by the scene unload.

## Patterns

- **UI screens**: use the screen's MonoBehaviour as owner; closing (destroying) the screen frees its sprites and prefabs.
- **Pooled objects**: spawn once with a long-lived owner (a pool component or a scope), reuse the instances, and dispose
  the owner when the pool is discarded. Do not `Despawn` pooled instances you intend to reuse.
- **Scene transitions**: use one `AddrScope` per scene; dispose it before loading the next scene.

## Supported versions and platforms

Unity 6 (6000.0 and newer) and Addressables 2.x. Developed on Unity 6000.3 with Addressables 2.6.

Verified by running a scripted 21-check smoke test inside a built player (every Addr API, incl. mid-load release,
cancellation, scenes, preload, 100-cycle churn):

| Target | Backend | Result |
|---|---|---|
| macOS standalone (development) | Mono | 21/21 pass |
| WebGL (release, high managed stripping) | IL2CPP | 21/21 pass, run in a browser |
| Android ARM64 (release, high stripping) | IL2CPP | builds (not run on a device) |

Not yet verified: Windows/Linux standalone, iOS, consoles, Unity versions other than 6000.3.
