using System;
using System.Collections.Generic;

namespace AddrKit
{
    /// <summary>
    /// Thrown when an AddrKit load fails. The message names the key and, when possible,
    /// suggests the address you probably meant.
    /// </summary>
    public sealed class AddrException : Exception
    {
        public string Key { get; }

        public AddrException(string key, string message, Exception inner = null)
            : base(message, inner)
        {
            Key = key;
        }

        internal static AddrException ForKey(object key, Exception cause)
        {
            string text = key?.ToString() ?? "<null>";
            string reason = cause?.Message ?? "unknown error";
            string message = $"Could not load '{text}': {reason}";

            if (key is string s)
            {
                var hints = AddrKeyMatcher.Closest(s, AddrKeyMatcher.AllKnownKeys());
                if (hints.Count > 0) message += $"\nDid you mean: {string.Join(", ", hints)}?";
            }
            return new AddrException(text, message, cause);
        }
    }

    /// <summary>Finds the address a developer most likely meant when a key is mistyped.</summary>
    public static class AddrKeyMatcher
    {
        /// <summary>Every string key known to the loaded Addressables catalogs.</summary>
        public static IEnumerable<string> AllKnownKeys()
        {
            foreach (var locator in UnityEngine.AddressableAssets.Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                    if (key is string s) yield return s;
            }
        }

        /// <summary>Returns up to <paramref name="max"/> candidates that are close to <paramref name="key"/>.</summary>
        public static List<string> Closest(string key, IEnumerable<string> candidates, int max = 3)
        {
            var scored = new List<(string text, int distance)>();
            int allowed = Math.Max(2, key.Length / 3);

            foreach (var candidate in candidates)
            {
                int distance = Distance(key.ToLowerInvariant(), candidate.ToLowerInvariant());
                if (distance <= allowed) scored.Add((candidate, distance));
            }

            scored.Sort((a, b) => a.distance.CompareTo(b.distance));
            var result = new List<string>();
            foreach (var item in scored)
            {
                if (result.Count == max) break;
                if (item.text != key && !result.Contains(item.text)) result.Add(item.text);
            }
            return result;
        }

        /// <summary>Levenshtein edit distance.</summary>
        public static int Distance(string a, string b)
        {
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) previous[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }
                (previous, current) = (current, previous);
            }
            return previous[b.Length];
        }
    }
}
