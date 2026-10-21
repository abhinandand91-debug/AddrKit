using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>
    /// Draws [AddrKey] string fields as a text box with a dropdown of existing addresses.
    /// A key that does not exist turns red and shows the closest real address underneath.
    /// </summary>
    [CustomPropertyDrawer(typeof(AddrKeyAttribute))]
    public sealed class AddrKeyDrawer : PropertyDrawer
    {
        static readonly Color MissingColor = new Color(1f, 0.45f, 0.4f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            return IsMissing(property) ? height + EditorGUIUtility.singleLineHeight : height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [AddrKey] on a string field.");
                return;
            }

            var attr = (AddrKeyAttribute)attribute;
            List<string> keys = CollectKeys(attr.Label);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var fieldRect = new Rect(line.x, line.y, line.width - 24, line.height);
            var buttonRect = new Rect(line.xMax - 22, line.y, 22, line.height);

            bool missing = !string.IsNullOrEmpty(property.stringValue) && !keys.Contains(property.stringValue);
            Color previous = GUI.contentColor;
            if (missing) GUI.contentColor = MissingColor;

            var content = new GUIContent(label.text, "Pick an Addressables address. Red means it does not exist.");
            property.stringValue = EditorGUI.TextField(fieldRect, content, property.stringValue);
            GUI.contentColor = previous;

            if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Keyboard))
                ShowMenu(buttonRect, property, keys);

            if (missing)
            {
                var hintRect = new Rect(position.x + EditorGUIUtility.labelWidth, line.yMax, position.width - EditorGUIUtility.labelWidth, line.height);
                List<string> closest = AddrKeyMatcher.Closest(property.stringValue, keys, 1);
                string hint = closest.Count > 0 ? $"Not found. Did you mean \"{closest[0]}\"?" : "Not found in Addressables.";
                EditorGUI.LabelField(hintRect, hint, EditorStyles.miniLabel);
            }
        }

        static void ShowMenu(Rect buttonRect, SerializedProperty property, List<string> keys)
        {
            var menu = new GenericMenu();
            if (keys.Count == 0) menu.AddDisabledItem(new GUIContent("No addresses found. Run Tools > AddrKit > Run Setup"));

            // Copy: the menu callback runs after OnGUI returns.
            SerializedProperty target = property.Copy();
            foreach (string key in keys)
            {
                string chosen = key;
                menu.AddItem(new GUIContent(key), key == property.stringValue, () =>
                {
                    target.stringValue = chosen;
                    target.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.DropDown(buttonRect);
        }

        bool IsMissing(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String || string.IsNullOrEmpty(property.stringValue)) return false;
            return !CollectKeys(((AddrKeyAttribute)attribute).Label).Contains(property.stringValue);
        }

        // The Inspector repaints constantly; rebuilding the list from all Addressables entries each time is wasteful.
        static readonly Dictionary<string, (double time, List<string> keys)> cache = new Dictionary<string, (double, List<string>)>();

        static List<string> CollectKeys(string label)
        {
            string cacheKey = label ?? "";
            if (cache.TryGetValue(cacheKey, out var hit) && EditorApplication.timeSinceStartup - hit.time < 1.0)
                return hit.keys;

            List<string> keys = BuildKeys(label);
            cache[cacheKey] = (EditorApplication.timeSinceStartup, keys);
            return keys;
        }

        static List<string> BuildKeys(string label)
        {
            var keys = new List<string>();
            var settings = AddrEditorUtil.AddressablesSettings;
            if (settings == null) return keys;

            foreach (var entry in AddrEditorUtil.AllEntries(settings))
                if (string.IsNullOrEmpty(label) || entry.labels.Contains(label))
                    keys.Add(entry.address);

            keys.Sort();
            return keys;
        }
    }
}
