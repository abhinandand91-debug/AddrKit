# Troubleshooting

| Symptom | Likely cause and fix |
|---|---|
| `AddrException: Could not load 'X'` | Wrong address. The message suggests close matches. Use `Keys.` constants to avoid typos. |
| `Keys` / `Labels` not found | Run `Tools > AddrKit > Generate Keys`, and add `using AddrKit.Generated;` (namespace is set in AddrKitSettings). |
| Changes to rules do nothing | Press **Apply Rules** or check the rule's folder, extensions list and that **enabled** is ticked. |
| An address is used twice | Two files map to the same address. Switch the rule to `RelativePath`, or add a prefix. Analyze shows it. |
| Spawn returns null | The owner was destroyed while the load was in flight. This is by design: the load is discarded. |
| Memory keeps growing | Open the Debug tab or press F9. Rows that never disappear have a global owner or a scope that was never disposed. |
| Works in the Editor, fails in a build | Build content first (`Tools > AddrKit > Build Content`) before building the player. |
| `GetDownloadSize` is not 0 for local content | Expected on WebGL: bundles are always fetched over HTTP there. Elsewhere it means the bundle is remote and not cached yet. |
| Remote content does not load | Use Existing Build, Remote On, server running, and the Server URL matches. Check the `ServerData/<platform>` folder exists. |
| Objects look pink/missing in bundles | A dependency (shader, material) was not included. Run Analyze and make shared dependencies addressable. |
| `Despawn: ... was not spawned by AddrKit` | You passed an object that came from `Instantiate`. Use `Destroy` for those. |

## Tips

- Name addresses like folders: `Enemies/Goblin`, `UI/Icons/Sword`.
- One owner per lifetime: a screen, a level, a character. Avoid global loads.
- Set the log level to **Verbose** in AddrKitSettings to see every load and release in the Console.
