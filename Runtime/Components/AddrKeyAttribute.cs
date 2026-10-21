using UnityEngine;

namespace AddrKit
{
    /// <summary>
    /// Put on a string field to pick an Addressables address from a dropdown in the Inspector.
    /// <code>[AddrKey] public string enemyKey;</code>
    /// Optionally limit the list to one label: <c>[AddrKey("enemy")]</c>.
    /// </summary>
    public sealed class AddrKeyAttribute : PropertyAttribute
    {
        public string Label { get; }

        public AddrKeyAttribute(string label = null)
        {
            Label = label;
        }
    }
}
