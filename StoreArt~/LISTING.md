# Asset Store listing (draft)

Copy these into the Publisher Portal. Art is in `out/` (regenerate with `python3 make_art.py`).

## Basics
- **Title:** AddrKit
- **Technical name (reserve this):** `com.abhinandan.addrkit`
- **Category:** Tools > Utilities (or Editor Extensions)
- **Tags:** addressables, assets, loading, memory, leak, debugger, async, tools
- **Supported Unity:** 6000.0+ (developed and tested on 6000.3). **Render pipelines:** all (no shaders or pipeline assets).
- **Dependencies (must be disclosed, guideline 1.1.c):** Unity Addressables 2.x (`com.unity.addressables`, installed from the Unity Registry automatically).

## Short description (one line)
Addressables without the leaks: one-line loading, automatic release by owner, generated typed keys, a leak debugger and one-click rules.

## Long description
**Stop writing Release calls.** AddrKit sits on top of Unity Addressables and makes the safe way the easy way.

```csharp
var goblin = await Addr.Spawn(Keys.Enemies.Goblin, owner: this);
```
When `this` is destroyed, the goblin is released. Destroy a scope, a screen or a level and everything it loaded goes with it.

**Key features**
- **Owner-based auto release**: pass `this` or an `AddrScope`; nothing leaks, even if the owner dies mid-load.
- **Typed keys**: `Keys.Enemies.Goblin` is generated from your addresses, so typos fail to compile.
- **Folder rules**: point a rule at a folder and everything inside gets an address, group and labels, with a live preview.
- **Leak debugger**: live list of every loaded asset with owner, age and the line of code that loaded it. F9 overlay in the Editor and development builds.
- **Analyzer with one-click fixes**: duplicated dependencies, duplicate addresses, stale keys, oversized assets.
- **Safe async**: cancellation tokens, clear errors with "did you mean" suggestions, preload with progress.
- **Inspector tools**: `[AddrKey]` address dropdown that flags and fixes typos, no-code Spawner and Scene Loader components.
- **Remote content testing**: built-in local server for downloadable bundles.
- **Documented lifecycle**: exactly who owns what, and what happens on cancel or destroy.

**Works on**: macOS standalone (Mono), WebGL (IL2CPP, high stripping), Android (IL2CPP). Verified with an in-player smoke test of every API.
Not yet verified: Windows/Linux standalone, iOS.

**Includes**: source code, documentation (quick start, concepts, lifecycle contract, troubleshooting), Quick Start and Demo samples, edit mode and play mode test suites.

## Disclosures to put in the listing
- Requires Unity Addressables 2.x.
- The Demo sample generates its content and scene with a menu command (Tools > AddrKit > Set Up Demo), so it works in any render pipeline.
- Validator note: fill in after running the official Validator (see CHECKLIST.md).

## Support
- Documentation: README and `Documentation~/` in the package, also on GitHub.
- Contact: _add your support email_.
