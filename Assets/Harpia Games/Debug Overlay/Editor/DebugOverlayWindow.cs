using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Harpia.Tools.DebugTools
{
    public class DebugOverlayWindow : EditorWindow
    {
        private enum Tab
        {
            Main,
            Settings,
            Support
        }

        private Tab _tab = Tab.Main;
        private Vector2 _scrollPosition;
        private bool _showOnGameView;
        private double _lastRepaint;
        private string _search = "";

        private GUIStyle _keyStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _descStyle;
        private bool _stylesBuilt;

        private GUIStyle _previewStyle;
        private int _previewFontSize = -1;
        private Color _previewColor;
        private bool _previewMono;

        private static readonly Color ZebraColorPro = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color ZebraColorLight = new Color(0f, 0f, 0f, 0.04f);
        private static readonly Color SeparatorPro = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color SeparatorLight = new Color(0f, 0f, 0f, 0.15f);

        [MenuItem(DebugOverlay.MenuRoot + "Editor Window", false, 1)]
        public static void ShowWindow()
        {
            DebugOverlayWindow window = GetWindow<DebugOverlayWindow>("Debug Overlay");
            window.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Debug Overlay");
            _showOnGameView = DebugOverlay.GetShowOnGameView();
            DebugOverlaySettings.Load();
            EditorApplication.update += UpdateWindow;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateWindow;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= UpdateWindow;
        }

        private void UpdateWindow()
        {
            // Repaint the window at ~10 frames per second.
            // EditorApplication.timeSinceStartup is valid in edit mode; Time.realtimeSinceStartup is not.
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRepaint < 0.1) return;
            _lastRepaint = now;
            Repaint();
        }

        private void BuildStyles()
        {
            if (_stylesBuilt) return;

            _keyStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip
            };

            _valueStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = false,
                clipping = TextClipping.Clip
            };

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                margin = new RectOffset(2, 2, 4, 2)
            };

            _descStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            Color dim = _descStyle.normal.textColor;
            dim.a = 0.65f;
            _descStyle.normal.textColor = dim;

            _stylesBuilt = true;
        }

        private void OnGUI()
        {
            BuildStyles();

            // Apply the monospace preference to the shared entry styles each frame (cheap).
            Font entryFont = DebugOverlaySettings.Monospace ? DebugOverlaySettings.MonoFont : null;
            _keyStyle.font = entryFont;
            _valueStyle.font = entryFont;

            DrawTabs();

            switch (_tab)
            {
                case Tab.Main:
                    DrawMainToolbar();
                    DrawMainView();
                    break;
                case Tab.Settings:
                    DrawSettings();
                    break;
                case Tab.Support:
                    DrawSupport();
                    break;
            }
        }

        // ---- Tabs (first row) --------------------------------------------------

        private void DrawTabs()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawTabButton(Tab.Main, "Main", "Live debug values, buttons and variables coming from DebugOverlay at runtime.");
                DrawTabButton(Tab.Settings, "Settings", "Configure how the Game-view overlay looks: layout, colors, font.");
                DrawTabButton(Tab.Support, "Support", "Documentation, Discord, website and other Harpia Games assets.");
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawTabButton(Tab tab, string label, string tooltip)
        {
            bool isOn = _tab == tab;
            bool pressed = GUILayout.Toggle(isOn, new GUIContent(label, tooltip), EditorStyles.toolbarButton, GUILayout.Width(90));
            if (pressed && !isOn)
            {
                _tab = tab;
                GUIUtility.keyboardControl = 0;
            }
        }

        // ---- Main tab ----------------------------------------------------------

        private void DrawMainToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _showOnGameView = GUILayout.Toggle(_showOnGameView, new GUIContent("Game View", "Show or hide the debug overlay in the Game view."), EditorStyles.toolbarButton, GUILayout.Width(80));
                DebugOverlay.SetShowOnGameView(_showOnGameView);

                if (GUILayout.Button(new GUIContent("Clear", "Remove all entries and variables."), EditorStyles.toolbarButton, GUILayout.Width(50)))
                    DebugOverlay.Clear();

                if (GUILayout.Button(new GUIContent("Copy", "Copy every entry and variable to the system clipboard."), EditorStyles.toolbarButton, GUILayout.Width(46)))
                    CopyAllToClipboard();

                bool sort = GUILayout.Toggle(DebugOverlaySettings.SortAlphabetical, new GUIContent("A→Z", "Sort entries and variables alphabetically by key."), EditorStyles.toolbarButton, GUILayout.Width(44));
                if (sort != DebugOverlaySettings.SortAlphabetical)
                {
                    DebugOverlaySettings.SortAlphabetical = sort;
                    DebugOverlaySettings.Save();
                }

                GUILayout.FlexibleSpace();

                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(80), GUILayout.MaxWidth(180));
                if (GUILayout.Button(new GUIContent("×", "Clear the search filter."), EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    _search = "";
                    GUIUtility.keyboardControl = 0;
                }
            }
        }

        private void DrawMainView()
        {
            Dictionary<string, DebugData> toShow = DebugOverlay._toShow;
            Dictionary<string, DebugVariables> toVars = DebugOverlay._toShowVariables;

            int dataCount = toShow?.Count ?? 0;
            int varCount = toVars?.Count ?? 0;

            if (dataCount == 0 && varCount == 0)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("No debug data yet.\nCall DebugOverlay.Show(...), ShowButton(...) or ShowVariable(...) at runtime.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"{dataCount} entr{(dataCount == 1 ? "y" : "ies")}   ·   {varCount} variable{(varCount == 1 ? "" : "s")}", EditorStyles.miniLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (dataCount > 0)
                DrawEntries(toShow);

            if (varCount > 0)
            {
                EditorGUILayout.Space(6);
                DrawSeparator();
                EditorGUILayout.LabelField("Variables", _sectionStyle);
                DrawVariables(toVars);
            }

            EditorGUILayout.EndScrollView();
        }

        private void CopyAllToClipboard()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (DebugOverlay._toShow != null)
                foreach (KeyValuePair<string, DebugData> e in DebugOverlay._toShow)
                    sb.AppendLine($"{e.Key}: {e.Value.text}");

            if (DebugOverlay._toShowVariables != null)
                foreach (KeyValuePair<string, DebugVariables> e in DebugOverlay._toShowVariables)
                    sb.AppendLine($"{e.Key}: {e.Value.currentTextFieldInput}");

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            ShowNotification(new GUIContent("Copied to clipboard"));
        }

        // ---- Settings tab ------------------------------------------------------

        private void DrawSettings()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Overlay Settings", _sectionStyle);
            DrawSeparator();
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Layout", EditorStyles.miniBoldLabel);
            DebugOverlaySettings.Anchor = (OverlayAnchor)EditorGUILayout.EnumPopup(new GUIContent("Anchor", "Screen corner the overlay is pinned to."), DebugOverlaySettings.Anchor);
            DebugOverlaySettings.OffsetX = EditorGUILayout.FloatField(new GUIContent("Offset X", "Horizontal distance (px) from the anchored corner."), DebugOverlaySettings.OffsetX);
            DebugOverlaySettings.OffsetY = EditorGUILayout.FloatField(new GUIContent("Offset Y", "Vertical distance (px) from the anchored corner."), DebugOverlaySettings.OffsetY);
            DebugOverlaySettings.LineWidth = EditorGUILayout.Slider(new GUIContent("Line Width", "Width (px) of each overlay row."), DebugOverlaySettings.LineWidth, 100f, 1600f);
            DebugOverlaySettings.LineHeight = EditorGUILayout.Slider(new GUIContent("Line Height", "Height (px) of each overlay row."), DebugOverlaySettings.LineHeight, 12f, 48f);
            DebugOverlaySettings.FontSize = EditorGUILayout.IntSlider(new GUIContent("Font Size", "Text size of overlay entries."), DebugOverlaySettings.FontSize, 8, 40);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Appearance", EditorStyles.miniBoldLabel);
            DebugOverlaySettings.TextColor = EditorGUILayout.ColorField(new GUIContent("Text Color", "Default color for entry text (per-entry colors still win)."), DebugOverlaySettings.TextColor);
            DebugOverlaySettings.Monospace = EditorGUILayout.Toggle(new GUIContent("Monospace Font", "Use a fixed-width font so numbers and vectors line up."), DebugOverlaySettings.Monospace);
            DebugOverlaySettings.ShowBackground = EditorGUILayout.Toggle(new GUIContent("Show Background", "Draw a filled background strip behind each row."), DebugOverlaySettings.ShowBackground);
            using (new EditorGUI.DisabledScope(!DebugOverlaySettings.ShowBackground))
                DebugOverlaySettings.BackgroundColor = EditorGUILayout.ColorField(new GUIContent("Background Color", "Color (and alpha) of the row background."), DebugOverlaySettings.BackgroundColor);

            if (EditorGUI.EndChangeCheck())
            {
                DebugOverlaySettings.Save();
                Repaint(); // reflect the edit in the live preview immediately
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button(new GUIContent("Reset to Defaults", "Restore all overlay settings to their default values."), GUILayout.Height(24)))
            {
                DebugOverlaySettings.ResetToDefaults();
                Repaint();
            }

            // Live preview — drawn AFTER the controls so it reflects this frame's edits.
            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);
            DrawPreview();

            EditorGUILayout.EndScrollView();
        }

        // ---- Support tab -------------------------------------------------------

        private void DrawSupport()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Help", _sectionStyle);
            DrawSeparator();
            EditorGUILayout.Space(2);

            if (GUILayout.Button(new GUIContent("Documentation", "Open the online documentation in your browser."), GUILayout.Height(24)))
                Application.OpenURL(DebugOverlay.DocumentationUrl);
            if (GUILayout.Button(new GUIContent("Discord", "Join the Harpia Games community for support."), GUILayout.Height(24)))
                Application.OpenURL(DebugOverlay.DiscordUrl);
            if (GUILayout.Button(new GUIContent("Website", "Visit harpiagames.com."), GUILayout.Height(24)))
                Application.OpenURL(DebugOverlay.WebsiteUrl);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Support the Developer", _sectionStyle);
            DrawSeparator();
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox("This tool is free. If it helps you, consider buying me a coffee — thank you!", MessageType.None);
            if (GUILayout.Button(new GUIContent("☕  Buy Me a Coffee", "Support the developer at buymeacoffee.com/brunolorenz11."), GUILayout.Height(28)))
                Application.OpenURL(DebugOverlay.BuyMeACoffeeUrl);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("More Assets", _sectionStyle);
            DrawSeparator();
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Debug Overlay is a free tool — if you liked it, check out my other tools:", _descStyle);
            EditorGUILayout.Space(4);

            DrawAssetCard("Prefab Brush", "Easy object placement & level design tool.", DebugOverlay.PrefabBrushUrl);
            DrawAssetCard("PerfectShot", "Capture marketing shots & icons for your assets.", DebugOverlay.PerfectShotUrl);
            DrawAssetCard("Quick Animation Events", "Manage animation events quickly and easily.", DebugOverlay.QuickAnimationEventsUrl);
            DrawAssetCard("Better Delete", "Clean your project & find asset references.", DebugOverlay.BetterDeleteUrl);

            EditorGUILayout.Space(6);
            if (GUILayout.Button(new GUIContent("See more tools at harpiagames.com", "Visit harpiagames.com for more tools."), GUILayout.Height(28)))
                Application.OpenURL(DebugOverlay.WebsiteUrl);

            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetCard(string title, string description, string url)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(description, _descStyle);
                }

                if (GUILayout.Button(new GUIContent("View", title), GUILayout.Width(60), GUILayout.Height(36)))
                    Application.OpenURL(url);
            }
            EditorGUILayout.Space(3);
        }

        private static readonly string[] PreviewLines =
        {
            "Sample Text: Hello world",
            "Sample Int: 42",
            "Sample Variable 1: 1"
        };

        private void DrawPreview()
        {
            float rowH = DebugOverlaySettings.LineHeight;
            float totalH = rowH * PreviewLines.Length + 6;
            Rect area = GUILayoutUtility.GetRect(10, totalH, GUILayout.ExpandWidth(true));

            // Frame + optional overlay background, mirroring runtime look.
            EditorGUI.DrawRect(area, EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.25f) : new Color(0f, 0f, 0f, 0.08f));
            if (DebugOverlaySettings.ShowBackground)
                EditorGUI.DrawRect(area, DebugOverlaySettings.BackgroundColor);

            // Rebuild the cached style only when font size, text color, or monospace changes.
            if (_previewStyle == null || _previewFontSize != DebugOverlaySettings.FontSize || _previewColor != DebugOverlaySettings.TextColor || _previewMono != DebugOverlaySettings.Monospace)
            {
                _previewFontSize = DebugOverlaySettings.FontSize;
                _previewColor = DebugOverlaySettings.TextColor;
                _previewMono = DebugOverlaySettings.Monospace;
                _previewStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = _previewFontSize,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = _previewColor },
                    font = _previewMono ? DebugOverlaySettings.MonoFont : null
                };
            }

            float y = area.y + 3;
            foreach (string line in PreviewLines)
            {
                GUI.Label(new Rect(area.x + 6, y, area.width - 12, rowH), line, _previewStyle);
                y += rowH;
            }
        }

        private void DrawEntries(Dictionary<string, DebugData> toShow)
        {
            Color zebra = EditorGUIUtility.isProSkin ? ZebraColorPro : ZebraColorLight;
            int row = 0;

            // Clone before enumerating: the play-mode overlay mutates these dictionaries.
            List<KeyValuePair<string, DebugData>> entries = new List<KeyValuePair<string, DebugData>>(toShow);
            if (DebugOverlaySettings.SortAlphabetical)
                entries.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            foreach (KeyValuePair<string, DebugData> entry in entries)
            {
                if (!PassesFilter(entry.Key)) continue;

                DebugData data = entry.Value;

                using (EditorGUILayout.HorizontalScope h = new EditorGUILayout.HorizontalScope())
                {
                    if (Event.current.type == EventType.Repaint && (row & 1) == 1)
                        EditorGUI.DrawRect(h.rect, zebra);

                    DrawColorSwatch(data.c);

                    if (data.action != null)
                    {
                        if (GUILayout.Button(new GUIContent(entry.Key, "Click to invoke this debug action."), EditorStyles.miniButton))
                            InvokeSafe(entry.Key, data.action);
                    }
                    else if (data.refTransform != null)
                    {
                        if (GUILayout.Button(new GUIContent("Select  " + entry.Key, "Select this GameObject in the Hierarchy."), EditorStyles.miniButton))
                            Selection.activeGameObject = data.refTransform.gameObject;
                    }
                    else
                    {
                        GUILayout.Label(new GUIContent(entry.Key, entry.Key), _keyStyle, GUILayout.Width(150));
                        GUILayout.Label(new GUIContent(data.text, data.text), _valueStyle, GUILayout.ExpandWidth(true));
                    }
                }

                row++;
            }
        }

        private void DrawVariables(Dictionary<string, DebugVariables> toVars)
        {
            List<KeyValuePair<string, DebugVariables>> entries = new List<KeyValuePair<string, DebugVariables>>(toVars);
            if (DebugOverlaySettings.SortAlphabetical)
                entries.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            foreach (KeyValuePair<string, DebugVariables> entry in entries)
            {
                if (!PassesFilter(entry.Key)) continue;

                DebugVariables dv = entry.Value;
                string newInput = EditorGUILayout.TextField(new GUIContent(entry.Key, "Editable debug variable — changes fire its onChanged callback."), dv.currentTextFieldInput);
                if (newInput != dv.currentTextFieldInput)
                {
                    dv.currentTextFieldInput = newInput;
                    InvokeSafe(entry.Key, () => dv.onChanged?.Invoke(newInput), "onChanged");
                }
            }
        }

        private void DrawColorSwatch(Color c)
        {
            Rect r = GUILayoutUtility.GetRect(10, 16, GUILayout.Width(10), GUILayout.ExpandWidth(false));
            r.y += 4;
            r.height = 8;
            r.width = 8;
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(r, c.a <= 0f ? Color.white : c);
        }

        private void DrawSeparator()
        {
            Rect r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, EditorGUIUtility.isProSkin ? SeparatorPro : SeparatorLight);
        }

        private bool PassesFilter(string key)
        {
            if (string.IsNullOrEmpty(_search)) return true;
            return key != null && key.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void InvokeSafe(string key, Action action, string label = "Action")
        {
            if (action == null) return;
            try { action.Invoke(); }
            catch (Exception e) { Debug.LogError($"[DebugOverlay] {label} '{key}' threw: {e}"); }
        }
    }
}
