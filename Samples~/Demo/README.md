# AddrKit Demo

A playable tour of AddrKit: spawning, scopes, labels, auto-release, scenes, preloading and the leak overlay.

1. Run **Tools > AddrKit > Set Up Demo** (once). It creates sample content, rules and a scene in `Assets/AddrKitDemoContent`
   and opens the scene. Works in Built-in, URP and HDRP projects.
2. Press **Play**.
3. Press **F9** in the Game view to see everything currently loaded. Click buttons and watch rows appear and disappear.
4. Click **Leak: global load** and note that its row never goes away until you press **Release globals**.
   That is what a memory leak looks like in AddrKit.

Open `AddrKitDemo.cs` next: each button is one or two lines of AddrKit code.
