# Changelog

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
