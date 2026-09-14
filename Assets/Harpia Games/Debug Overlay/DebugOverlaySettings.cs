using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Harpia.Tools.DebugTools
{
    /// <summary>Screen corner the overlay anchors to.</summary>
    public enum OverlayAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// User-tunable look of the DebugOverlay overlay. Persisted per-machine via EditorPrefs.
    /// Overlay is editor-only, so prefs access is wrapped in UNITY_EDITOR; builds use defaults.
    /// </summary>
    public static class DebugOverlaySettings
    {
        private const string Prefix = "DebugOverlay.";

        // Defaults
        public static readonly Color DefaultTextColor = Color.white;
        public static readonly Color DefaultBackgroundColor = new Color(0f, 0f, 0f, 0.75f);
        private const int DefFontSize = 12;
        private const float DefLineHeight = 18f;
        private const float DefLineWidth = 600f;
        private const float DefOffsetX = 10f;
        private const float DefOffsetY = 5f;
        private const bool DefShowBackground = true;
        private const bool DefSortAlphabetical = false;
        private const bool DefMonospace = false;
        private const OverlayAnchor DefAnchor = OverlayAnchor.TopLeft;

        public static int FontSize = DefFontSize;
        public static float LineHeight = DefLineHeight;
        public static float LineWidth = DefLineWidth;
        public static float OffsetX = DefOffsetX;
        public static float OffsetY = DefOffsetY;
        public static bool ShowBackground = DefShowBackground;
        public static bool SortAlphabetical = DefSortAlphabetical;
        public static bool Monospace = DefMonospace;
        public static OverlayAnchor Anchor = DefAnchor;

        private static Font _monoFont;

        /// <summary>Lazily-created monospace font (OS-provided), shared by overlay + window.</summary>
        public static Font MonoFont
        {
            get
            {
                if (_monoFont == null)
                    _monoFont = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Menlo", "monospace" }, 12);
                return _monoFont;
            }
        }
        public static Color TextColor = Color.white;
        public static Color BackgroundColor = new Color(0f, 0f, 0f, 0.75f);

        private static bool _loaded;

        /// <summary>
        /// Clears the loaded guard so the next <see cref="Load"/> re-reads EditorPrefs.
        /// Needed for Fast Enter Play mode, where statics survive between Play sessions.
        /// </summary>
        public static void ResetLoadState() => _loaded = false;

        /// <summary>Loads persisted values (editor only). Safe to call repeatedly.</summary>
        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

#if UNITY_EDITOR
            FontSize = EditorPrefs.GetInt(Prefix + "FontSize", DefFontSize);
            LineHeight = EditorPrefs.GetFloat(Prefix + "LineHeight", DefLineHeight);
            LineWidth = EditorPrefs.GetFloat(Prefix + "LineWidth", DefLineWidth);
            OffsetX = EditorPrefs.GetFloat(Prefix + "OffsetX", DefOffsetX);
            OffsetY = EditorPrefs.GetFloat(Prefix + "OffsetY", DefOffsetY);
            ShowBackground = EditorPrefs.GetBool(Prefix + "ShowBackground", DefShowBackground);
            SortAlphabetical = EditorPrefs.GetBool(Prefix + "SortAlphabetical", DefSortAlphabetical);
            Monospace = EditorPrefs.GetBool(Prefix + "Monospace", DefMonospace);
            Anchor = (OverlayAnchor)EditorPrefs.GetInt(Prefix + "Anchor", (int)DefAnchor);
            TextColor = LoadColor("TextColor", DefaultTextColor);
            BackgroundColor = LoadColor("BackgroundColor", DefaultBackgroundColor);
#endif
        }

        /// <summary>Persists current values (editor only).</summary>
        public static void Save()
        {
#if UNITY_EDITOR
            EditorPrefs.SetInt(Prefix + "FontSize", FontSize);
            EditorPrefs.SetFloat(Prefix + "LineHeight", LineHeight);
            EditorPrefs.SetFloat(Prefix + "LineWidth", LineWidth);
            EditorPrefs.SetFloat(Prefix + "OffsetX", OffsetX);
            EditorPrefs.SetFloat(Prefix + "OffsetY", OffsetY);
            EditorPrefs.SetBool(Prefix + "ShowBackground", ShowBackground);
            EditorPrefs.SetBool(Prefix + "SortAlphabetical", SortAlphabetical);
            EditorPrefs.SetBool(Prefix + "Monospace", Monospace);
            EditorPrefs.SetInt(Prefix + "Anchor", (int)Anchor);
            SaveColor("TextColor", TextColor);
            SaveColor("BackgroundColor", BackgroundColor);
#endif
        }

        /// <summary>Restores defaults and persists them.</summary>
        public static void ResetToDefaults()
        {
            FontSize = DefFontSize;
            LineHeight = DefLineHeight;
            LineWidth = DefLineWidth;
            OffsetX = DefOffsetX;
            OffsetY = DefOffsetY;
            ShowBackground = DefShowBackground;
            SortAlphabetical = DefSortAlphabetical;
            Monospace = DefMonospace;
            Anchor = DefAnchor;
            TextColor = DefaultTextColor;
            BackgroundColor = DefaultBackgroundColor;
            Save();
        }

#if UNITY_EDITOR
        private static Color LoadColor(string key, Color fallback)
        {
            string s = EditorPrefs.GetString(Prefix + key, "");
            if (!string.IsNullOrEmpty(s) && ColorUtility.TryParseHtmlString("#" + s, out Color c))
                return c;
            return fallback;
        }

        private static void SaveColor(string key, Color c)
        {
            EditorPrefs.SetString(Prefix + key, ColorUtility.ToHtmlStringRGBA(c));
        }
#endif
    }
}
