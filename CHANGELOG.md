# Changelog

## Unreleased

- New **Demo** sample: import it, run `Tools > AddrKit > Set Up Demo`, press Play, press F9. Generates its own content, rules and scene for any render pipeline.
- Real-build verification: 21-check smoke test passes in a macOS Mono player and a WebGL IL2CPP (high stripping) player; Android IL2CPP ARM64 builds.
- Docs: `GetDownloadSize` / Preload behavior on WebGL, verified platforms table.

## 0.1.1

- Fixed: releasing an owner (or disposing a scope) while a load was in flight made the caller's task throw
  "invalid operation handle". It now completes with null and leaves nothing behind.
- Added `CancellationToken` to `Load`, `LoadAll`, `Spawn`, `SpawnUnder`, `LoadScene`.
- Added `Addr.SpawnAll(label, owner)`.
- Analyzer reports stale generated Keys.
- New `Documentation~/Lifecycle.md` (ownership, cancellation, preload semantics).
- 8 new tests (mid-load release, double dispose, shared assets, repeated cycles, cancellation, SpawnAll, deterministic keys).

## 0.1.0

First release.

- `Addr` facade: `Load`, `LoadAll`, `Spawn`, `SpawnUnder`, `LoadScene`, `Preload`, `GetDownloadSize`, `UpdateCatalogs`, `Despawn`, `Unload`, `ReleaseAll`.
- Owner-based release: pass `this` or an `AddrScope`; loads are released when the owner is destroyed.
- Readable errors with "did you mean" suggestions.
- Generated `Keys` and `Labels` classes.
- Folder rules (`AddrRuleSet`) with auto-apply.
- `AddrKit` window: Setup, Rules, Keys, Analyze, Build (incl. local server for remote testing), Debug.
- `[AddrKey]` Inspector dropdown, `AddrSpawner`, `AddrSceneLoader`, F9 debug overlay.
- Edit mode and play mode test suites.
