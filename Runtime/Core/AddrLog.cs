using UnityEngine;

namespace AddrKit
{
    /// <summary>Console output for AddrKit, filtered by <see cref="AddrSettings.logLevel"/>.</summary>
    internal static class AddrLog
    {
        const string Prefix = "[AddrKit] ";

        public static void Verbose(string message)
        {
            if (AddrSettings.Current.logLevel >= AddrLogLevel.Verbose) Debug.Log(Prefix + message);
        }

        public static void Warn(string message)
        {
            if (AddrSettings.Current.logLevel >= AddrLogLevel.Warnings) Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            if (AddrSettings.Current.logLevel >= AddrLogLevel.Errors) Debug.LogError(Prefix + message);
        }
    }
}
