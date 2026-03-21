# Unity Editor Navigator

A Unity Editor tool for reorganizing GameObjects in the Hierarchy and moving assets in the Project window, all without touching the mouse.

---

## Features

- Reorder and reparent GameObjects in the Hierarchy using keyboard shortcuts
- Move assets to any folder through a keyboard-driven move mode
- Full undo support (`Ctrl+Z`)
- Works with multi-selection

---

## Requirements

- Unity 6000.1 or later

---

## Installation

### Option A — Unity Package Manager (recommended)

1. Open **Window → Package Manager**
2. Click the **+** button → **Add package from git URL...**
3. Enter the repository URL and click **Add**

### Option B — Manual

1. Download or clone this repository
2. Copy the `unity-editor-navigator` folder into your project's `Packages/` directory
3. Unity will automatically detect and import the package

---

## How It Works

### Hierarchy Navigator

Select one or more GameObjects in the Hierarchy window and use the shortcuts below.

| Shortcut | Action |
|---|---|
| `Ctrl + Shift + ↑` | Move selected object(s) up |
| `Ctrl + Shift + ↓` | Move selected object(s) down |
| `Ctrl + Shift + →` | Parent to the sibling above (move in) |
| `Ctrl + Shift + ←` | Unparent (move out to parent's level) |

### Project Navigator — Move Mode

Moving assets uses a two-phase workflow:

1. Select one or more assets in the Project window
2. Press `Ctrl + Shift + ↑` or `↓` to enter Move Mode
3. A green-highlighted folder appears as the target destination
4. Navigate to the desired folder:
   - `↑` / `↓` — browse sibling folders at the current level
   - `→` — go into the highlighted folder
   - `←` — go up one level
5. Press `Enter` to confirm the move, or `Escape` to cancel

---

## Settings & Help

Access the full shortcut reference at any time via **Tools → Hierarchy Navigator → Settings & Help**.

---

## Author

Made by [mhze](mailto:mhze.uk@gmail.com)
