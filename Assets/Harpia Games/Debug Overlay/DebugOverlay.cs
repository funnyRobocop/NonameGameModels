using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Harpia.Tools.DebugTools
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
    [SuppressMessage("Domain reload", "UDR0005:Domain Reload Analyzer")]
    public class DebugOverlay : MonoBehaviour
    {
        private static bool _show = true;
        private const string Prefskey = "DebugOverlay_enabled";

        /// <summary>Root path for all Debug Overlay menu items.</summary>
        public const string MenuRoot = "Tools/Harpia Games/Debug Overlay/";

        public static Dictionary<string, DebugData> _toShow = new();
        public static Dictionary<string, DebugVariables> _toShowVariables = new();
        public static HashSet<string> toClear = new();

        private static DebugOverlay _instace1;

        private static bool _showOnGameView = true;

        // Cached overlay styles, rebuilt when the configured font size / monospace changes.
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private int _builtFontSize = -1;
        private bool _builtMono;

        // Reused per-frame render buffers (avoids allocating dictionaries every OnGUI).
        private readonly List<KeyValuePair<string, DebugData>> _renderData = new();
        private readonly List<KeyValuePair<string, DebugVariables>> _renderVars = new();

#if UNITY_EDITOR

        /// <summary>Resets static state on play.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void StaticInit()
        {
            _toShowVariables = new Dictionary<string, DebugVariables>();
            _toShow = new Dictionary<string, DebugData>();
            toClear = new HashSet<string>();
            _showOnGameView = true;
            _instace1 = null;
            _show = true;
            DebugOverlaySettings.ResetLoadState();
            DebugOverlaySettings.Load();
        }

        /// <summary>Enables the overlay.</summary>
        [MenuItem(MenuRoot + "Enable", false, 20)]
        public static void SetCanShowTrue() => SetShow(true);

        [MenuItem(MenuRoot + "Enable", true)]
        private static bool ValidateEnable()
        {
            Menu.SetChecked(MenuRoot + "Enable", _show);
            return true;
        }

        /// <summary>Disables the overlay.</summary>
        [MenuItem(MenuRoot + "Disable", false, 21)]
        public static void SetCanShowFalse() => SetShow(false);

        [MenuItem(MenuRoot + "Disable", true)]
        private static bool ValidateDisable()
        {
            Menu.SetChecked(MenuRoot + "Disable", !_show);
            return true;
        }

        /// <summary>Adds sample entries for preview.</summary>
        [MenuItem(MenuRoot + "Add Samples", false, 23)]
        public static void ShowSamples()
        {
            Show("Sample Text", "Hello world");
            Show("Sample Int", 42);
            Show("Sample Float", 3.14159f);
            Show("Sample Bool", true, Color.green);
            Show("Sample Vector3", new Vector3(1f, 2f, 3f));
            ShowButton("Sample Action", () => Debug.Log("[DebugOverlay] Sample action clicked"), Color.cyan);
            ShowVariable("Sample Variable 1", "1");
            ShowVariable("Sample Variable 2", "text");
        }

        /// <summary>Removes all entries and variables.</summary>
        [MenuItem(MenuRoot + "Clear Logs", false, 22)]
#endif
        public static void Clear()
        {
            _toShow?.Clear();
            _toShowVariables?.Clear();
            toClear?.Clear();
        }

        /// <summary>Removes a single entry.</summary>
        /// <param name="key">Entry identifier.</param>
        public static void Remove(string key)
        {
            _toShow?.Remove(key);
            toClear?.Remove(key);
        }

        /// <summary>Removes a single variable.</summary>
        /// <param name="key">Variable identifier.</param>
        public static void RemoveVariable(string key) => _toShowVariables?.Remove(key);

        #region Query helpers (handy for tests / MCP / automation)

        /// <summary>True if entry key exists.</summary>
        /// <param name="key">Entry identifier.</param>
        public static bool Contains(string key) => _toShow != null && _toShow.ContainsKey(key);

        /// <summary>Entry text, or null.</summary>
        /// <param name="key">Entry identifier.</param>
        public static string GetValue(string key)
            => _toShow != null && _toShow.TryGetValue(key, out DebugData d) ? d.text : null;

        /// <summary>Tries to read an entry.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Found text output.</param>
        public static bool TryGetValue(string key, out string value)
        {
            value = null;
            if (_toShow == null || !_toShow.TryGetValue(key, out DebugData d)) return false;
            value = d.text;
            return true;
        }

        /// <summary>Variable input, or null.</summary>
        /// <param name="key">Variable identifier.</param>
        public static string GetVariable(string key)
            => _toShowVariables != null && _toShowVariables.TryGetValue(key, out DebugVariables v) ? v.currentTextFieldInput : null;

        /// <summary>All current entry keys.</summary>
        public static string[] GetKeys()
            => _toShow == null ? Array.Empty<string>() : _toShow.Keys.ToArray();

        /// <summary>Snapshot of everything as strings.</summary>
        public static Dictionary<string, string> GetSnapshot()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_toShow != null)
                foreach (KeyValuePair<string, DebugData> kv in _toShow)
                    result[kv.Key] = kv.Value.text;
            if (_toShowVariables != null)
                foreach (KeyValuePair<string, DebugVariables> kv in _toShowVariables)
                    result[kv.Key] = kv.Value.currentTextFieldInput;
            return result;
        }

        #endregion

#if UNITY_EDITOR

        #region MenuItem_TopMenu

        // Free tool. Support links + a "More Assets" section listing other Harpia Games
        // tools (mirrors Top Menu Shortcuts). Affiliate URLs kept in sync with that asset.
        public const string DocumentationUrl = "https://harpiagames.com";
        public const string DiscordUrl = "https://discord.gg/Tr952uhsqb";
        public const string WebsiteUrl = "https://harpiagames.com";
        public const string BuyMeACoffeeUrl = "https://buymeacoffee.com/brunolorenz11";

        public const string PrefabBrushUrl = "https://assetstore.unity.com/packages/tools/level-design/prefab-brush-easy-object-placement-tool-level-designer-260560?clickref=1101lDzbJ98g&utm_source=partnerize&utm_medium=affiliate&utm_campaign=unity_affiliate";
        public const string PerfectShotUrl = "https://assetstore.unity.com/packages/tools/camera/perfectshot-marketing-icons-editor-tool-340274?clickref=1100lDaFBJ68&utm_source=partnerize&utm_medium=affiliate&utm_campaign=unity_affiliate";
        public const string QuickAnimationEventsUrl = "https://prf.hn/click/camref:1100lACye/destination:https://assetstore.unity.com/packages/tools/animation/quick-animation-events-manage-animation-events-easily-311920";
        public const string BetterDeleteUrl = "https://assetstore.unity.com/packages/tools/utilities/better-delete-project-cleaner-references-finder-344536?clickref=1101lDzbJqfC&utm_source=partnerize&utm_medium=affiliate&utm_campaign=unity_affiliate";

        [MenuItem(MenuRoot + "Documentation", false, 100)]
        private static void OpenDocumentation() => Application.OpenURL(DocumentationUrl);

        [MenuItem(MenuRoot + "Discord", false, 111)]
        private static void OpenDiscord() => Application.OpenURL(DiscordUrl);

        [MenuItem(MenuRoot + "Buy Me a Coffee", false, 112)]
        private static void OpenBuyMeACoffee() => Application.OpenURL(BuyMeACoffeeUrl);

        [MenuItem(MenuRoot + "More Assets/Prefab Brush - Level Design", false, 120)]
        private static void AssetPrefabBrush() => Application.OpenURL(PrefabBrushUrl);

        [MenuItem(MenuRoot + "More Assets/PerfectShot - Marketing & Icons", false, 121)]
        private static void AssetPerfectShot() => Application.OpenURL(PerfectShotUrl);

        [MenuItem(MenuRoot + "More Assets/Quick Animation Events", false, 122)]
        private static void AssetAnimationEvents() => Application.OpenURL(QuickAnimationEventsUrl);

        [MenuItem(MenuRoot + "More Assets/Better Delete - Project Cleaner", false, 123)]
        private static void AssetBetterDelete() => Application.OpenURL(BetterDeleteUrl);

        [MenuItem(MenuRoot + "More Assets/Harpia Games Website", false, 134)]
        private static void AssetWebsite() => Application.OpenURL(WebsiteUrl);

        #endregion

#endif

        private static void SpawnObject()
        {
            if (_instace1 != null) return;
            GameObject go = new GameObject("Debug Object");
            _instace1 = go.AddComponent<DebugOverlay>();

#if UNITY_EDITOR
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.HideAndDontSave;
            _show = EditorPrefs.GetBool(Prefskey, true);
#else
            _show = true;
#endif

            //Onsceneloaded event
            SceneManager.sceneLoaded += (_, _) =>
            {
                if (_instace1 == null) return;
                Clear();
            };

            Application.quitting += () =>
            {
                Destroy(go);
                _toShow.Clear();
            };
        }

        /// <summary>Shows a quaternion value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="q">Quaternion to display.</param>
        public static void Show(string key, Quaternion q)
        {
            if (Application.isEditor == false) return;
            const string precision = "F3";
            Show(key, "x: " + q.x.ToString(precision) + " y: " + q.y.ToString(precision) + " z: " + q.z.ToString(precision) + " w: " + q.w.ToString(precision));
        }

        /// <summary>Shows a transform's name.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="t">Transform to display.</param>
        public static void Show(string key, Transform t)
        {
            if (Application.isEditor == false) return;

            Show(key, t == null ? "null" : t.name);
        }

        /// <summary>Shows a boolean value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="v">Boolean to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, bool v, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, v ? "True" : "False", c);
        }

        /// <summary>Shows multiple booleans, comma separated.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="v">Booleans to display.</param>
        public static void Show(string key,  [NotNull] params bool[] v)
        {
            if (Application.isEditor == false) return;
            string defaultString = string.Join(", ", v.Select(b => b ? "True" : "False"));
            Show(key, defaultString,  default);
        }

        /// <summary>Shows a component's name and state.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="mb">Component to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, MonoBehaviour mb, Color c = default)
        {
            if (Application.isEditor == false) return;

            var name = mb == null ? "null" : mb.gameObject.name;
            bool isActive = mb != null && mb.gameObject.activeInHierarchy;
            name += isActive ? " (Active)" : " (Inactive)";
            Show(key, name, c);
        }

        /// <summary>Shows a GameObject's name and state.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="go">GameObject to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, GameObject go, Color c = default)
        {
            if (Application.isEditor == false) return;

            var name = go == null ? "null" : go.name;
            bool isActive = go != null && go.activeInHierarchy;
            name += isActive ? " (Active)" : " (Inactive)";
            Show(key, name, c);
        }

        /// <summary>Shows a float value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Float to display.</param>
        /// <param name="decimals">Decimal places to show.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, float value, int decimals = 3, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, value.ToString($"F{decimals}"), c);
        }

        /// <summary>Shows a double value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Double to display.</param>
        /// <param name="decimals">Decimal places to show.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, double value, int decimals = 3, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, value.ToString($"F{decimals}"), c);
        }

        /// <summary>Shows an integer value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Integer to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, int value, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, value.ToString(), c);
        }

        /// <summary>Shows the current frame count.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="c">Optional text color.</param>
        public static void ShowLastFrame(string key, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, Time.frameCount, c);
        }

        /// <summary>Shows a string value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Text to display.</param>
        /// <param name="c">Optional text color.</param>
        /// <param name="clearNextFrame">Remove after this frame.</param>
        public static void Show(string key, string value, Color c = default, bool clearNextFrame = false)
        {
            if (Application.isEditor == false) return;
            SpawnObject();

            if (c == default) c = DebugOverlaySettings.TextColor;

            if (_toShow.TryGetValue(key, out _))
                _toShow[key] = new(value, c);
            else
                _toShow.Add(key, new(value, c));

            if (clearNextFrame)
            {
                toClear ??= new HashSet<string>();
                toClear.Add(key);
            }
        }

        /// <summary>Draws the overlay each frame.</summary>
        public void OnGUI()
        {
            if (!_show) return;
            if (!_showOnGameView) return;
            if (Application.isEditor == false) return;
            if (_toShow.Count == 0 && _toShowVariables.Count == 0) return;

            BuildStyle();

            float rowH = DebugOverlaySettings.LineHeight;
            float lineWidth = DebugOverlaySettings.LineWidth;
            bool drawBg = DebugOverlaySettings.ShowBackground;
            Color bgColor = DebugOverlaySettings.BackgroundColor;

            // Snapshot into reused buffers so callbacks invoked below can mutate the source
            // dictionaries safely, without allocating a new dictionary every frame.
            _renderData.Clear();
            foreach (KeyValuePair<string, DebugData> kv in _toShow) _renderData.Add(kv);
            _renderVars.Clear();
            foreach (KeyValuePair<string, DebugVariables> kv in _toShowVariables) _renderVars.Add(kv);

            if (DebugOverlaySettings.SortAlphabetical)
            {
                _renderData.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
                _renderVars.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            }

            int totalRows = _renderData.Count + _renderVars.Count;

            // Cap visible rows to what fits on screen; reserve one line for the overflow notice.
            int maxRows = Mathf.Max(1, Mathf.FloorToInt((Screen.height - DebugOverlaySettings.OffsetY * 2f) / rowH));
            bool overflow = totalRows > maxRows;
            int budget = overflow ? maxRows - 1 : totalRows;
            int displayedRows = overflow ? maxRows : totalRows;
            float totalHeight = displayedRows * rowH;

            OverlayAnchor anchor = DebugOverlaySettings.Anchor;
            bool right = anchor is OverlayAnchor.TopRight or OverlayAnchor.BottomRight;
            bool bottom = anchor is OverlayAnchor.BottomLeft or OverlayAnchor.BottomRight;

            float x = right ? Screen.width - lineWidth - DebugOverlaySettings.OffsetX : DebugOverlaySettings.OffsetX;
            float y = bottom ? Mathf.Max(0f, Screen.height - totalHeight - DebugOverlaySettings.OffsetY) : DebugOverlaySettings.OffsetY;

            Color prevColor = GUI.color;
            Color prevBg = GUI.backgroundColor;
            int drawn = 0;

            foreach (KeyValuePair<string, DebugData> pair in _renderData)
            {
                if (drawn >= budget) break;

                DebugData pairValue = pair.Value;
                Color textColor = pairValue.c;
                Rect position = new Rect(x, y, lineWidth, rowH);
                y += rowH;
                drawn++;

                string keyString = pair.Key;

                if (pairValue.refTransform != null)
                {
                    if (DrawButton(position, "Select  " + keyString, textColor))
                    {
#if UNITY_EDITOR
                        Selection.activeGameObject = pairValue.refTransform.gameObject;
                        Debug.Log($"[DebugOverlay] Showing transform {keyString}", pairValue.refTransform);
#endif
                    }

                    continue;
                }

                if (pairValue.action != null)
                {
                    if (DrawButton(position, keyString, textColor))
                    {
                        try
                        {
                            pairValue.action.Invoke();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[DebugOverlay] Action '{keyString}' threw: {e}");
                        }
                    }

                    continue;
                }

                if (drawBg)
                {
#if UNITY_EDITOR
                    EditorGUI.DrawRect(position, bgColor);
#endif
                }

                Rect textRect = position;
                textRect.x += 4;
                GUI.color = textColor;
                GUI.Label(textRect, $"{keyString}: {pairValue}", _labelStyle);
                GUI.color = prevColor;
            }

            // Entries flagged clearNextFrame were rendered above; remove them now so
            // they disappear next frame unless re-shown. Runs once, after the render loop.
            if (toClear is { Count: > 0 })
            {
                foreach (string s in toClear) _toShow.Remove(s);
                toClear.Clear();
            }

            foreach (KeyValuePair<string, DebugVariables> pair in _renderVars)
            {
                if (drawn >= budget) break;

                Rect rowRect = new Rect(x, y, lineWidth, rowH);
                y += rowH;
                drawn++;

                if (drawBg)
                {
#if UNITY_EDITOR
                    EditorGUI.DrawRect(rowRect, bgColor);
#endif
                }

                float labelW = lineWidth * 0.4f;
                GUI.color = DebugOverlaySettings.TextColor;
                Rect keyRect = new Rect(x + 4, rowRect.y, labelW - 4, rowH);
                GUI.Label(keyRect, pair.Key, _labelStyle);
                GUI.color = prevColor;

                DebugVariables dv = pair.Value;
                Rect fieldRect = new Rect(x + labelW, rowRect.y, lineWidth - labelW, rowH);
                string newInput = GUI.TextField(fieldRect, dv.currentTextFieldInput, 25);
                if (newInput != dv.currentTextFieldInput)
                {
                    dv.currentTextFieldInput = newInput;
                    try
                    {
                        dv.onChanged?.Invoke(newInput);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[DebugOverlay] Variable '{pair.Key}' onChanged threw: {e}");
                    }
                }
            }

            if (overflow)
            {
                Rect r = new Rect(x, y, lineWidth, rowH);
                if (drawBg)
                {
#if UNITY_EDITOR
                    EditorGUI.DrawRect(r, bgColor);
#endif
                }

                GUI.color = DebugOverlaySettings.TextColor;
                GUI.Label(new Rect(x + 4, y, lineWidth - 4, rowH), $"… +{totalRows - drawn} more", _labelStyle);
                GUI.color = prevColor;
            }

            GUI.color = prevColor;
            GUI.backgroundColor = prevBg;
        }

        /// <summary>Rebuilds cached overlay styles when the configured font size changes.</summary>
        private void BuildStyle()
        {
            bool mono = DebugOverlaySettings.Monospace;
            if (_labelStyle != null && _builtFontSize == DebugOverlaySettings.FontSize && _builtMono == mono) return;
            _builtFontSize = DebugOverlaySettings.FontSize;
            _builtMono = mono;
            Font font = mono ? DebugOverlaySettings.MonoFont : null;
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _builtFontSize,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                font = font
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = _builtFontSize,
                font = font
            };
        }

        /// <summary>Draws a tinted overlay button; returns true when clicked.</summary>
        private bool DrawButton(Rect r, string label, Color tint)
        {
            Color prevBg = GUI.backgroundColor;
            Color prevColor = GUI.color;
            tint.a = 1f;
            GUI.backgroundColor = tint;
            GUI.color = Color.white;
            bool clicked = GUI.Button(r, label, _buttonStyle);
            GUI.backgroundColor = prevBg;
            GUI.color = prevColor;
            return clicked;
        }

        /// <summary>Shows a list of strings.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="list">Strings to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, List<string> list, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, string.Join(", ", list), c);
        }

        /// <summary>Shows a Vector2 value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Vector to display.</param>
        /// <param name="decimals">Decimal places to show.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, Vector2 value, int decimals = 4, Color c = default)
        {
            string format = "f" + decimals;
            Show(key, $"({value.x.ToString(format)}, {value.y.ToString(format)})", c);
        }

        /// <summary>Shows a Vector3 value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Vector to display.</param>
        /// <param name="decimals">Decimal places to show.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, Vector3 value, int decimals = 4, Color c = default)
        {
            string format = "f" + decimals;
            Show(key, $"({value.x.ToString(format)}, {value.y.ToString(format)}, {value.z.ToString(format)})", c);
        }

        /// <summary>Shows a color as RGBA.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Color to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, Color value, Color c = default)
        {
            if (Application.isEditor == false) return;
            // Tint the text with the color itself (opaque) when no explicit text color is given.
            Color textColor = c == default ? new Color(value.r, value.g, value.b, 1f) : c;
            Show(key, $"RGBA({value.r:F3}, {value.g:F3}, {value.b:F3}, {value.a:F3})", textColor);
        }

        /// <summary>Shows a long value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Long to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, long value, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, value.ToString(), c);
        }

        /// <summary>Shows an enum value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Enum to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, Enum value, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, value == null ? "null" : value.ToString(), c);
        }

        /// <summary>Shows a Vector3Int value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Vector to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, Vector3Int value, Color c = default)
        {
            if (Application.isEditor == false) return;
            Show(key, $"({value.x}, {value.y}, {value.z})", c);
        }

        /// <summary>Shows a Rect value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Rect to display.</param>
        /// <param name="decimals">Decimal places to show.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, Rect value, int decimals = 2, Color c = default)
        {
            if (Application.isEditor == false) return;
            string f = "f" + decimals;
            Show(key, $"(x:{value.x.ToString(f)}, y:{value.y.ToString(f)}, w:{value.width.ToString(f)}, h:{value.height.ToString(f)})", c);
        }

        /// <summary>Shows a Bounds value.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Bounds to display.</param>
        /// <param name="decimals">Decimal places to show.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, Bounds value, int decimals = 2, Color c = default)
        {
            if (Application.isEditor == false) return;
            string f = "f" + decimals;
            string center = $"({value.center.x.ToString(f)}, {value.center.y.ToString(f)}, {value.center.z.ToString(f)})";
            string size = $"({value.size.x.ToString(f)}, {value.size.y.ToString(f)}, {value.size.z.ToString(f)})";
            Show(key, $"center:{center} size:{size}", c);
        }

        /// <summary>Shows a quaternion with decimals.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="value">Quaternion to display.</param>
        /// <param name="decimals">Decimal places to show.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string key, Quaternion value, int decimals = 4, Color c = default)
        {
            string format = "f" + decimals;
            Show(key, $"({value.x.ToString(format)}, {value.y.ToString(format)}, {value.z.ToString(format)},  {value.w.ToString(format)})", c);
        }

        /// <summary>Adds an editable text-field variable.</summary>
        /// <param name="key">Variable identifier.</param>
        /// <param name="initial">Starting text value.</param>
        /// <param name="onChanged">Fires when edited.</param>
        public static string ShowVariable(string key, string initial = "", Action<string> onChanged = null)
        {
            if (Application.isEditor == false) return initial;
            SpawnObject();

            if (_toShowVariables.TryGetValue(key, out DebugVariables existing))
                return existing.currentTextFieldInput;

            _toShowVariables.Add(key, new DebugVariables(initial, onChanged));
            return initial;
        }

        /// <summary>Toggles drawing in the Game view.</summary>
        /// <param name="n">True to show overlay.</param>
        public static void SetShowOnGameView(bool n)
        {
            _showOnGameView = n;
        }

        /// <summary>True if drawing in Game view.</summary>
        public static bool GetShowOnGameView() => _showOnGameView;

        /// <summary>Enables or disables the overlay.</summary>
        /// <param name="b">True to enable.</param>
        public static void SetShow(bool b)
        {
            _show = b;

            if (!_show) Clear();

#if UNITY_EDITOR
            EditorPrefs.SetBool(Prefskey, b);
#endif
        }

        /// <summary>Shows a Vector2Int value.</summary>
        /// <param name="n">Entry identifier.</param>
        /// <param name="val">Vector to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void Show(string n, Vector2Int val, Color c = default)
        {
            Show(n, $"{val.x}, {val.y}", c);
        }

        /// <summary>Green if true, red otherwise.</summary>
        /// <param name="isTrue">Condition to color.</param>
        public static Color GetAssertionColor(bool isTrue)
        {
            return isTrue ? Color.green : Color.red;
        }

        /// <summary>Adds a button that selects a Transform.</summary>
        /// <param name="key">Button label.</param>
        /// <param name="cameraParent">Transform to select.</param>
        /// <param name="c">Optional button color.</param>
        public static void ShowButton(string key, Transform cameraParent, Color c = default)
        {
            if (Application.isEditor == false) return;
            SpawnObject();
            DebugData data = new DebugData(key, cameraParent, c);
            _toShow[key] = data;
        }

        /// <summary>Adds a button that invokes a callback.</summary>
        /// <param name="key">Button label.</param>
        /// <param name="onClick">Action to invoke.</param>
        /// <param name="c">Optional button color.</param>
        public static void ShowButton(string key, Action onClick, Color c = default)
        {
            if (Application.isEditor == false) return;
            if (onClick == null) return;
            SpawnObject();
            _toShow[key] = new DebugData(key, onClick, c);
        }

        /// <summary>Shows each element of an array.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="array">Components to display.</param>
        /// <param name="c">Optional text color.</param>
        public static void ShowArray(string key, MonoBehaviour[] array, Color c = default)
        {
            if (array == null)
            {
                Show(key, "null array");
                return;
            }

            Show(key + " Count", array.Length, 0);

            for (int i = 0; i < array.Length; i++)
            {
                Show($"{key} [{i}]", array[i], c);
            }
        }

        /// <summary>Shows any object via ToString.</summary>
        /// <param name="key">Object to display.</param>
        public static void Show(object key)
        {
            if (Application.isEditor == false) return;
            Show(key == null ? "null" : key.ToString(), key == null ? "null" : key.ToString());
        }

        /// <summary>Shows a list of components.</summary>
        /// <param name="key">Entry identifier.</param>
        /// <param name="l">Components to display.</param>
        /// <param name="c">Text color.</param>
        /// <param name="clearNextFrame">Remove after this frame.</param>
        public static void Show(string key, List<Component> l, Color c, bool clearNextFrame = false)
        {
#if UNITY_EDITOR
            string value = l == null ? "null" : string.Join(", ", l.Select(c1 => c1 == null ? "null" : c1.gameObject.name));
            Show(key, value, c, clearNextFrame);
#endif
        }

        /// <summary>Shows a list of colliders.</summary>
        /// <param name="itpFoundedVisible">Entry identifier.</param>
        /// <param name="visibles">Colliders to display.</param>
        /// <param name="decimals">Text color.</param>
        /// <param name="clearNextFrame">Remove after this frame.</param>
        public static void Show(string itpFoundedVisible, List<Collider> visibles, Color decimals, bool clearNextFrame)
        {
            if (Application.isEditor == false) return;
            if (visibles == null)
            {
                Show(itpFoundedVisible, "null", decimals, clearNextFrame);
                return;
            }

            Show(itpFoundedVisible, visibles.Select(e => e as Component).ToList(), decimals, clearNextFrame);
        }

        /// <summary>Shows a raycast hit summary.</summary>
        /// <param name="itpFoundedVisible">Entry identifier.</param>
        /// <param name="h">Raycast hit to display.</param>
        public static void Show(string itpFoundedVisible, RaycastHit h)
        {
            Show(itpFoundedVisible, $"P: {h.point} N: {h.normal} D: {h.distance:F3}");
        }
    }

    public class DebugVariables
    {
        public string text;
        public string currentTextFieldInput;
        public Action<string> onChanged;

        public DebugVariables(string text, Action<string> onChanged = null)
        {
            this.text = text;
            currentTextFieldInput = text;
            this.onChanged = onChanged;
        }
    }

    public class DebugData
    {
        public string text;
        public Color c;
        public readonly Transform refTransform;
        public readonly Action action;

        public DebugData(string text, Color c)
        {
            this.text = text;
            this.c = c == Color.clear ? Color.white : c;
        }

        public DebugData(string key, Transform t, Color c)
        {
            refTransform = t;
            this.c = c == Color.clear ? Color.white : c;
            text = key;
        }

        public DebugData(string label, Action action, Color c)
        {
            this.action = action;
            this.c = c == Color.clear ? Color.white : c;
            text = label;
        }

        public override string ToString() => text;
    }
}
