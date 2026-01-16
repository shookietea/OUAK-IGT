# OUAK-IGT

In-Game Timer (IGT) overlay mod for **Once Upon A KATAMARI**.

## Features

- **IGT tracking** - Displays in-game time directly from the game value
- **Repositionable Timer** - Move to your preferred location
- **Persistent settings** - Timer position saved automatically
- **On-screen notifications** - Visual feedback for actions
- **Color-coded indicators** - Timer color changes depending on situation

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/) for Once Upon A KATAMARI
2. Place `OUAK_IGT.dll` in `<Game Directory>\Mods\`
3. Launch the game

## Usage

### Hotkeys (Default)

- **F8** - Toggle overlay visibility
- **F9** - Toggle move mode
- **F10** - Reset position to default

### Moving the Timer

1. Press **F9** to enter move mode
2. Click and drag the timer to desired position
3. Press **F9** again to exit move mode

Position is saved automatically and persists across sessions.

### Visual Feedback

The timer changes color to indicate its current state:
- **Off-white** - Normal operation, timer is running
- **Cyan** - Move mode is active, timer can be dragged

## Configuration

When `DisableKeyBinds = true`, the timer will still display and persist your previously saved position.

Edit `<Game Directory>/UserData/MelonPreferences.cfg` to customize:

```ini
[IGT_Settings]
TimerPositionX = 20.0          # Timer X position (0 = left edge)
TimerPositionY = -20.0         # Timer Y position (negative = down from top)
TimerFontSize = 72.0          # Timer font size (affects timer dimensions, scales automatically)
DisableKeyBinds = false       # Set to true to disable F8/F9/F10 hotkeys (timer remains visible)
HotDogTurtleMode = true       # Show centiseconds (MM:SS.cc) instead of milliseconds (MM:SS.mmm)
ToggleKey = "F8"              # Key to toggle overlay visibility
MoveKey = "F9"                # Key to toggle move mode
ResetKey = "F10"              # Key to reset position to default position
```

### Position Coordinate System

- **X axis**: 0 (left edge) to screen width (right edge)
- **Y axis**: 0 (top edge) to negative screen height (bottom edge)
- Default (20, -20) places timer 20 pixels from top-left corner
- Timer is clamped to screen boundaries automatically

### Valid Key Names

Use Unity KeyCode names (case-insensitive):
- Function keys: `F1` - `F24`
- [Full KeyCode list](https://docs.unity3d.com/ScriptReference/KeyCode.html)

## Troubleshooting

**Timer is not visible:**
- Press F8 to toggle visibility (if keybinds not disabled)
- Check MelonLoader console for errors
- Verify the mod DLL is in the Mods folder
- Press F10 to reset position to default

**Move mode won't activate:**
- Check if `DisableKeyBinds = true` in your config
- Try configuring a different KeyCode if F9 conflicts
- Check MelonLoader console for errors

**Timer text looks weird, is cutoff, is offscreen, etc.**
- Configure another font size
- Report the bug with your configuration

## Building from Source

### Prerequisites

- .NET 6.0 SDK
- Once Upon A KATAMARI (Steam) with MelonLoader installed

### Setup

1. Clone the repository
2. Set the `OUAK_PATH` environment variable to your game installation directory
3. Build with `dotnet build OUAK-IGT.csproj --configuration Release`
4. The DLL will automatically copy to your Mods folder

## Changelog

**1.6.0** - Initial release
- Working IGT display
- Configurable keybinds (F8/F9/F10)
- Dynamic font sizing
- Boundary protection
- Position persistence
- Color-coded move mode indicator
