using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Draws [AddrKey] string fields as a text box with a dropdown of existing addresses.</summary>
    [CustomPropertyDrawer(typeof(AddrKeyAttribute))]
    public sealed class AddrKeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [AddrKey] on a string field.");
                return;
            }

            var attr = (AddrKeyAttribute)attribute;
            var keys = CollectKeys(attr.Label);

            var fieldRect = new Rect(position.x, position.y, position.width - 24, position.height);
            var buttonRect = new Rect(position.xMax - 22, position.y, 22, position.height);

            // Red text means the address does not exist (yet), a likely typo.
            bool missing = !string.IsNullOrEmpty(property.stringValue) && !keys.Contains(property.stringValue);
            Color previous = GUI.contentColor;
            if (missing) GUI.contentColor = new Color(1f, 0.45f, 0.4f);
            property.stringValue = EditorGUI.TextField(fieldRect, label, property.stringValue);
            GUI.contentColor = previous;

            if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Keyboard))
            {
                var menu = new GenericMenu();
                if (keys.Count == 0) menu.AddDisabledItem(new GUIContent("No addresses found"));
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
        }

        static List<string> CollectKeys(string label)
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
