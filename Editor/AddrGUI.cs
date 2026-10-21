using System;
using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    public enum Tone
    {
        Neutral,
        Good,
        Warn,
        Bad,
        Info
    }

    /// <summary>
    /// Small drawing kit shared by every AddrKit editor page: cards, status pills, headers and buttons.
    /// Colors are chosen to read on both the dark and light Editor skins.
    /// </summary>
    internal static class AddrGUI
    {
        static GUIStyle card, cardTitle, headerTitle, headerSub, pill, mono, hint, bigButton, rowTitle;

        static readonly Color Accent = new Color(0.55f, 0.78f, 1.00f);

        public static Color ToneColor(Tone tone)
        {
            switch (tone)
            {
                case Tone.Good: return new Color(0.20f, 0.60f, 0.34f);
                case Tone.Warn: return new Color(0.82f, 0.56f, 0.08f);
                case Tone.Bad: return new Color(0.78f, 0.26f, 0.26f);
                case Tone.Info: return new Color(0.24f, 0.48f, 0.80f);
                default: return new Color(0.42f, 0.42f, 0.46f);
            }
        }

        // ---------------------------------------------------------------- styles (created lazily; they need EditorStyles)

        public static GUIStyle CardStyle => card ??= new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(4, 4, 2, 6)
        };

        static GUIStyle CardTitle => cardTitle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        static GUIStyle HeaderTitle => headerTitle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
        static GUIStyle HeaderSub => headerSub ??= new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 11, normal = { textColor = Color.gray } };
        public static GUIStyle RowTitle => rowTitle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };

        public static GUIStyle Hint => hint ??= new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
            normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.62f, 0.62f, 0.64f) : new Color(0.38f, 0.38f, 0.40f) }
        };

        public static GUIStyle Mono => mono ??= new GUIStyle(EditorStyles.textArea)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Menlo", "Consolas", "Courier New" }, 11),
            fontSize = 11,
            wordWrap = false
        };

        static GUIStyle Pill => pill ??= new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            padding = new RectOffset(8, 8, 1, 2)
        };

        static GUIStyle BigButton => bigButton ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 0
        };

        // ---------------------------------------------------------------- building blocks

        /// <summary>Page title with a one-line explanation under it.</summary>
        public static void Header(string title, string subtitle)
        {
            GUILayout.Space(4);
            GUILayout.Label(title, HeaderTitle);
            if (!string.IsNullOrEmpty(subtitle)) GUILayout.Label(subtitle, HeaderSub);
            GUILayout.Space(6);
        }

        /// <summary>A bordered box with an optional bold title. Use in a using block.</summary>
        public sealed class CardScope : GUI.Scope
        {
            public CardScope(string title = null)
            {
                EditorGUILayout.BeginVertical(CardStyle);
                if (!string.IsNullOrEmpty(title))
                {
                    GUILayout.Label(title, CardTitle);
                    GUILayout.Space(2);
                }
            }

            protected override void CloseScope() => EditorGUILayout.EndVertical();
        }

        /// <summary>A small rounded status label ("Ready", "3 warnings"). Sits inline in a horizontal group.</summary>
        public static void DrawPill(string text, Tone tone)
        {
            var content = new GUIContent(text);
            Vector2 size = Pill.CalcSize(content);
            Rect rect = GUILayoutUtility.GetRect(size.x + 6, 18, GUILayout.ExpandWidth(false));
            rect.y += 1;
            rect.height = 17;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, ToneColor(tone), Vector4.zero, new Vector4(8, 8, 8, 8));
            GUI.Label(rect, content, Pill);
            GUILayout.Space(3);
        }

        /// <summary>The one main action on a page: tall, blue, bold.</summary>
        public static bool PrimaryButton(string label, float height = 32)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = Accent;
            bool clicked = GUILayout.Button(label, BigButton, GUILayout.Height(height));
            GUI.backgroundColor = previous;
            return clicked;
        }

        public static bool Button(string label, float height = 24, params GUILayoutOption[] options)
        {
            var all = new GUILayoutOption[options.Length + 1];
            all[0] = GUILayout.Height(height);
            Array.Copy(options, 0, all, 1, options.Length);
            return GUILayout.Button(label, all);
        }

        /// <summary>A big number with a caption under it, for dashboards.</summary>
        public static void Stat(string caption, int value)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(70)))
            {
                GUILayout.Label(value.ToString(), new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
                GUILayout.Label(caption, new GUIStyle(Hint) { alignment = TextAnchor.MiddleCenter });
            }
        }

        public static void Separator()
        {
            Rect rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.25f));
            GUILayout.Space(4);
        }

        public static void OpenDocs() => Application.OpenURL("https://github.com/abhinandand91-debug/AddrKit#readme");
    }
}
