using UnityEditor.AddressableAssets.Settings;

namespace AddrKit.Editor
{
    /// <summary>Switches the project between "all content in the build" and "remote content from a server".</summary>
    public static class RemoteSetup
    {
        /// <summary>Folder (relative to the project) where remote bundles are built.</summary>
        public const string ServerFolder = "ServerData";

        /// <summary>
        /// Turns remote content on or off. Groups are made remote by ticking "remote" on a rule.
        /// <paramref name="baseUrl"/> is the server root, e.g. http://localhost:8080 or https://cdn.mygame.com/content.
        /// </summary>
        public static void Configure(AddressableAssetSettings settings, bool enable, string baseUrl)
        {
            settings.BuildRemoteCatalog = enable;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

            string buildPath = ServerFolder + "/[BuildTarget]";
            string loadPath = baseUrl.TrimEnd('/') + "/[BuildTarget]";
            var profiles = settings.profileSettings;
            profiles.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteBuildPath, buildPath);
            profiles.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, loadPath);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        }
    }
}
