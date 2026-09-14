# Debug Overlay — Documentation

Version 1.0 · Harpia Games
Website: https://harpiagames.com · Discord: https://discord.gg/Tr952uhsqb

A zero-setup, code-driven on-screen debug overlay for Unity. Call `DebugOverlay.Show(...)` from anywhere and the value appears in a corner of the Game view. Editor-only — it compiles out of builds, so you can leave the calls in your code.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Installation](#2-installation)
3. [Quick Start](#3-quick-start)
4. [Setup Guide (Step by Step)](#4-setup-guide-step-by-step)
5. [The Editor Window](#5-the-editor-window)
6. [Menu Items](#6-menu-items)
7. [Script Reference](#7-script-reference)
8. [Settings Reference](#8-settings-reference)
9. [Notes & Behavior](#9-notes--behavior)
10. [Support](#10-support)

---

## 1. Overview

Debug Overlay draws key/value pairs, buttons, and editable variables on top of the Game view during Play mode. It is meant for fast, throw-away debugging without building a UI.

- **No scene setup.** The overlay GameObject is spawned automatically on the first `Show` call.
- **Editor-only.** All display calls early-out when not in the editor, so leftover `Show` calls cost nothing in a build.
- **One static API.** Everything goes through the `DebugOverlay` class.

---

## 2. Installation

1. Import the package into your project (`Assets/Harpia Games/Debug Overlay/`).
2. Wait for Unity to compile. No further setup required.

The package has no dependencies and adds nothing to your scenes.

---

## 3. Quick Start

```csharp
using Harpia.Tools.DebugTools;
using UnityEngine;

public class Player : MonoBehaviour
{
    void Update()
    {
        DebugOverlay.Show("Position", transform.position);
        DebugOverlay.Show("Speed", 12.5f);
        DebugOverlay.Show("Grounded", true, Color.green);
    }
}
```

Press **Play**. The three lines appear in the top-left corner of the Game view.

---

## 4. Setup Guide (Step by Step)

1. **Import** the package (see [Installation](#2-installation)).
2. Open any script and add `using Harpia.Tools.DebugTools;`.
3. Add a `DebugOverlay.Show("key", value);` call — often in `Update()`.
4. Enter **Play mode**. The value shows on screen.
5. (Optional) Open **Tools ▸ Harpia Games ▸ Debug Overlay ▸ Editor Window** to tune font size, position, colors, and anchor.
6. (Optional) Toggle the overlay on/off with **Tools ▸ Harpia Games ▸ Debug Overlay ▸ Enable / Disable**.

That is the whole workflow. Repeat step 3 anywhere you want visibility.

---

## 5. The Editor Window

Open via **Tools ▸ Harpia Games ▸ Debug Overlay ▸ Editor Window**.

From it you can adjust: font size, line height/width, X/Y offset, background on/off, alphabetical sorting, monospace font, screen anchor, text color, and background color. Changes persist per-machine via `EditorPrefs`. A **Reset to Defaults** action restores factory values.

---

## 6. Menu Items

All under **Tools ▸ Harpia Games ▸ Debug Overlay**:

| Menu Item | Action |
|-----------|--------|
| Editor Window | Opens the settings window. |
| Enable | Turns the overlay on (checkmark shows current state). |
| Disable | Turns the overlay off. |
| Clear Logs | Removes all current entries and variables. |
| Add Samples | Adds example entries to preview the look. |
| Documentation / Discord / Buy Me a Coffee | Support links. |

---

## 7. Script Reference

Namespace: `Harpia.Tools.DebugTools` · Class: `DebugOverlay` (all members `static`).

Every `Show` takes a **`key`** (unique string identifier for the line) as the first argument. Calling `Show` again with the same key updates that line instead of adding a new one. Most overloads accept an optional `Color c` for the text.

### 7.1 Display values

```csharp
Show(string key, string value, Color c = default, bool clearNextFrame = false);
Show(string key, int value, Color c = default);
Show(string key, long value, Color c = default);
Show(string key, float value, int decimals = 3, Color c = default);
Show(string key, double value, int decimals = 3, Color c = default);
Show(string key, bool value, Color c = default);
Show(string key, params bool[] values);            // comma-separated
Show(string key, Enum value, Color c = default);
```

### 7.2 Unity types

```csharp
Show(string key, Vector2 value,    int decimals = 4, Color c = default);
Show(string key, Vector3 value,    int decimals = 4, Color c = default);
Show(string key, Vector2Int value, Color c = default);
Show(string key, Vector3Int value, Color c = default);
Show(string key, Quaternion value, int decimals = 4, Color c = default);
Show(string key, Rect value,       int decimals = 2, Color c = default);
Show(string key, Bounds value,     int decimals = 2, Color c = default);
Show(string key, Color value,      Color c = default);
Show(string key, Transform t);                     // shows name
Show(string key, GameObject go,   Color c = default);   // name + active state
Show(string key, MonoBehaviour mb, Color c = default);  // name + active state
Show(string key, RaycastHit hit);
```

### 7.3 Collections

```csharp
Show(string key, List<string> list, Color c = default);
Show(string key, List<Component> list, Color c, bool clearNextFrame = false);
Show(string key, List<Collider> visibles, Color c, bool clearNextFrame);
ShowArray(string key, MonoBehaviour[] array, Color c = default);
```

### 7.4 Buttons & editable variables

```csharp
// Draw a clickable button in the overlay.
ShowButton(string key, Action onClick, Color c = default);
ShowButton(string key, Transform cameraParent, Color c = default);

// Draw an editable text field; returns current value, onChanged fires on edit.
string ShowVariable(string key, string initial = "", Action<string> onChanged = null);
```

### 7.5 Management & queries

```csharp
Clear();                              // remove all entries + variables
Remove(string key);                   // remove one entry
RemoveVariable(string key);           // remove one variable
ShowLastFrame(string key, Color c = default);

bool   Contains(string key);
string GetValue(string key);          // entry text, or null
bool   TryGetValue(string key, out string value);
string GetVariable(string key);       // variable input, or null
string[] GetKeys();
Dictionary<string,string> GetSnapshot();   // everything as strings

void SetShow(bool b);                  // enable/disable overlay
void SetShowOnGameView(bool n);
bool GetShowOnGameView();
```

---

## 8. Settings Reference

Class: `DebugOverlaySettings` (static). Values persist per-machine via `EditorPrefs`.

| Field | Default | Meaning |
|-------|---------|---------|
| `FontSize` | 12 | Text size. |
| `LineHeight` | 18 | Vertical spacing per line. |
| `LineWidth` | 600 | Max line width. |
| `OffsetX` / `OffsetY` | 10 / 5 | Padding from the anchored corner. |
| `ShowBackground` | true | Draw a dark panel behind text. |
| `SortAlphabetical` | false | Sort entries by key. |
| `Monospace` | false | Use a monospace OS font. |
| `Anchor` | TopLeft | Screen corner (TopLeft/TopRight/BottomLeft/BottomRight). |
| `TextColor` | white | Default text color. |
| `BackgroundColor` | black 75% | Panel color. |

Methods: `Load()`, `Save()`, `ResetToDefaults()`, `ResetLoadState()`.

---

## 9. Notes & Behavior

- **Editor-only:** display calls return immediately outside the editor; safe to leave in shipped code.
- **Auto-spawn:** the overlay GameObject is created on first use and hidden in the hierarchy; it destroys itself on quit and clears on scene load.
- **Fast Enter Play mode safe:** static state is reset on each Play via `RuntimeInitializeOnLoadMethod`, so the overlay behaves correctly with Domain Reload disabled.
- **Same key = update:** reuse a key to update a line in place rather than stacking duplicates.

---

## 10. Support

- Website: https://harpiagames.com
- Discord: https://discord.gg/Tr952uhsqb
- ☕ Buy Me a Coffee: https://buymeacoffee.com/brunolorenz11

### More Tools by Harpia Games

- **Prefab Brush — Level Design:** https://assetstore.unity.com/packages/tools/level-design/prefab-brush-easy-object-placement-tool-level-designer-260560
- **PerfectShot — Marketing & Icons:** https://assetstore.unity.com/packages/tools/camera/perfectshot-marketing-icons-editor-tool-340274
- **Quick Animation Events:** https://assetstore.unity.com/packages/tools/animation/quick-animation-events-manage-animation-events-easily-311920
- **Better Delete — Project Cleaner:** https://assetstore.unity.com/packages/tools/utilities/better-delete-project-cleaner-references-finder-344536
