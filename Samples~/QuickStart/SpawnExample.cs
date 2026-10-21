using AddrKit;
using UnityEngine;

/// <summary>
/// The simplest possible use: spawn a prefab by address. Because 'this' is the owner,
/// the spawned object is released automatically when this component is destroyed.
/// Replace the key with one of your own addresses (or use the generated Keys class).
/// </summary>
public class SpawnExample : MonoBehaviour
{
    [AddrKey] public string prefabKey;     // pick from the dropdown in the Inspector

    async void Start()
    {
        try
        {
            GameObject instance = await Addr.Spawn(prefabKey, transform.position, Quaternion.identity, owner: this);
            if (instance != null) Debug.Log($"Spawned {instance.name}");   // null means this object was destroyed meanwhile
        }
        catch (AddrException e)
        {
            Debug.LogError(e.Message, this);   // for example a mistyped key
        }
    }
}
