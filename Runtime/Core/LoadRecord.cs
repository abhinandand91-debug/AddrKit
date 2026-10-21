using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddrKit
{
    public enum LoadKind
    {
        Asset,
        Assets,
        Instance,
        Scene
    }

    /// <summary>
    /// One thing AddrKit has loaded and is still responsible for releasing.
    /// The Debug window and overlay list these.
    /// </summary>
    public sealed class LoadRecord
    {
        public string Key { get; internal set; }
        public Type AssetType { get; internal set; }
        public LoadKind Kind { get; internal set; }
        public string OwnerName { get; internal set; }
        public float LoadedAtTime { get; internal set; }
        public string StackTrace { get; internal set; }

        internal object Owner;
        internal AsyncOperationHandle Handle;
        internal GameObject Instance;
        internal object Asset;
        internal bool Released;

        public bool IsDone => Handle.IsValid() && Handle.IsDone;
        public string Status => Handle.IsValid() ? Handle.Status.ToString() : "Released";
        public float Age => Time.realtimeSinceStartup - LoadedAtTime;

        public override string ToString() => $"{Kind} '{Key}' ({AssetType?.Name}) owned by {OwnerName}";
    }
}
