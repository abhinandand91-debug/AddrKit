using AddrKit;
using UnityEngine;

/// <summary>
/// Load several things for one "session" (a level, a menu, a battle) and release them together.
/// </summary>
public class ScopedLevelExample : MonoBehaviour
{
    [AddrKey] public string musicKey;
    [AddrKey] public string iconKey;

    AddrScope level;

    public async void StartLevel()
    {
        level = new AddrScope("Level");

        AudioClip music = await Addr.Load<AudioClip>(musicKey, level);
        Sprite icon = await Addr.Load<Sprite>(iconKey, level);
        Debug.Log($"Loaded {music.name} and {icon.name}");
    }

    public void EndLevel()
    {
        level?.Dispose();    // everything loaded with 'level' is released here
        level = null;
    }

    void OnDestroy() => EndLevel();
}
