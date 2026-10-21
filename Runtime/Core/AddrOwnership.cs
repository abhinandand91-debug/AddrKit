using System;
using System.Collections.Generic;
using UnityEngine;

namespace AddrKit
{
    /// <summary>
    /// A manual owner for loads that do not belong to one GameObject, such as "everything for level 1".
    /// <code>
    /// using var level = new AddrScope("Level 1");
    /// var boss = await Addr.Spawn(Keys.Enemies.Boss, level);
    /// // leaving the block releases everything loaded with 'level'
    /// </code>
    /// </summary>
    public sealed class AddrScope : IDisposable
    {
        public string Name { get; }

        public AddrScope(string name = "Scope")
        {
            Name = name;
        }

        public void Dispose() => Addr.ReleaseAll(this);

        public override string ToString() => $"Scope '{Name}'";
    }

    /// <summary>
    /// Hidden component added to a GameObject that owns loads. When the GameObject is destroyed
    /// it releases everything those owners loaded. You never add this yourself.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal sealed class AddrOwnerWatcher : MonoBehaviour
    {
        readonly List<object> owners = new List<object>();

        public static void WatchIfUnityObject(object owner)
        {
            GameObject target = owner switch
            {
                GameObject go => go,
                Component component => component != null ? component.gameObject : null,
                _ => null
            };
            if (target == null) return;

            var watcher = target.GetComponent<AddrOwnerWatcher>();
            if (watcher == null)
            {
                watcher = target.AddComponent<AddrOwnerWatcher>();
                watcher.hideFlags = HideFlags.HideInInspector;
            }
            if (!watcher.owners.Contains(owner)) watcher.owners.Add(owner);
        }

        void OnDestroy()
        {
            if (Addr.IsQuitting) return;
            foreach (object owner in owners) Addr.Service.ReleaseOwner(owner);
            owners.Clear();
        }
    }
}
