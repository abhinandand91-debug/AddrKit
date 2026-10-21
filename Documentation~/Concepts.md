# Concepts

## Address, label, key

- **Address**: the unique name of one asset, such as `Enemies/Goblin`.
- **Label**: a tag shared by many assets, such as `enemy`. Loading a label loads all of them.
- **Key**: either of the two. AddrKit functions say `key` or `labelOrKey`.

## Why not just reference the prefab in the Inspector?

A direct reference loads the prefab (and everything it uses) whenever the scene loads, forever.
An address loads it when you ask, and frees it when you are done. That is the entire point of Addressables.

## Handle, and why releasing matters

Every Addressables load returns a **handle** and bumps a counter on the asset's bundle.
The bundle only leaves memory when the counter hits zero. Forget one release and it stays forever.
AddrKit keeps every handle in a list, tagged with its **owner**, and releases them for you.

## Owner

The owner answers "who needs this?". Pass:

- `this` (a MonoBehaviour) or a GameObject: released when that object is destroyed.
- an `AddrScope`: released when you `Dispose()` it.
- nothing: global; released only by `Addr.Despawn`, `Addr.Unload`, `Addr.ReleaseAll(null)` or `Addr.ReleaseEverything()`.

Two owners can load the same address; the asset stays in memory until both have released.

## Group and bundle

A **group** is a folder in the Addressables system; assets in it are built into **bundles** (files).

- **Pack Together**: one bundle for the group. Fewer files, but loading one asset loads the bundle for all.
- **Pack Separately**: one bundle per asset. Finest memory control, more files.

A bundle is loaded or unloaded as a whole. Choose groups by "what is needed at the same time".

## Duplicates

If two bundles both use a texture that is not addressable, each bundle gets its own copy.
The Analyze tab lists these. Make the texture addressable and both bundles share one.

## Local and remote

- **Local**: bundles ship inside the build.
- **Remote**: bundles live on a server; the game downloads them on demand and caches them.
  Use for DLC, seasonal content, or keeping the first download small.

## Play mode scripts

How Addressables loads while you press Play in the Editor:

- **Use Asset Database**: reads assets directly. Fast, no build. Use daily.
- **Use Existing Build**: reads real bundles. Use to test what players get, and for remote content.

## What Keys generation does

AddrKit writes `Keys.cs` from your addresses so that `Keys.Enemies.Goblin` is checked by the compiler.
Rename an address and every stale use becomes a compile error you can fix, not a runtime failure.
