using UnityEngine;

namespace AddrKit
{
    public enum AddrLogLevel
    {
        Silent = 0,
        Errors = 1,
        Warnings = 2,
        Verbose = 3
    }

    /// <summary>
    /// Project-wide AddrKit options. Lives at Assets/AddrKit/Resources/AddrKitSettings.asset
    /// (create it with Tools > AddrKit > Run Setup). Without the asset, defaults are used.
    /// </summary>
    public sealed class AddrSettings : ScriptableObject
    {
        public const string ResourceName = "AddrKitSettings";

        [Header("Runtime")]
        [Tooltip("How much AddrKit writes to the Console.")]
        public AddrLogLevel logLevel = AddrLogLevel.Warnings;

        [Tooltip("Remember where each asset was loaded from (Editor and development builds only). Makes leak reports point at a line of code.")]
        public bool trackStackTraces = true;

        [Tooltip("Create the on-screen load overlay in the Editor and development builds.")]
        public bool createDebugOverlay = true;

        [Tooltip("Key that shows or hides the overlay.")]
        public KeyCode overlayKey = KeyCode.F9;

        [Header("Key generation (Editor)")]
        [Tooltip("Regenerate the Keys class whenever Addressables entries change.")]
        public bool autoGenerateKeys = true;

        [Tooltip("Where the generated Keys class is written.")]
        public string keysOutputPath = "Assets/AddrKit/Generated/Keys.cs";

        [Tooltip("Namespace of the generated Keys and Labels classes. Leave empty for the global namespace.")]
        public string keysNamespace = "AddrKit.Generated";

        [Header("Rules (Editor)")]
        [Tooltip("Re-apply folder rules automatically when assets in a rule folder change.")]
        public bool autoApplyRules = true;

        [Header("Analyzer (Editor)")]
        [Tooltip("The analyzer warns about assets bigger than this many megabytes.")]
        public int largeAssetWarningMB = 20;

        static AddrSettings current;

        /// <summary>The settings asset, or an in-memory default when none exists.</summary>
        public static AddrSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<AddrSettings>(ResourceName);
                if (current == null)
                {
                    current = CreateInstance<AddrSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => current = null;
    }
}
