using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(OUAK_IGT.IGT), "OUAK-IGT", "1.6.0", "shookie")]
[assembly: MelonGame("Bandai Namco Entertainment Inc.", "Once Upon A KATAMARI")]

namespace OUAK_IGT
{
    public class IGT : MelonMod
    {
        internal static MelonPreferences_Category _prefsCategory;
        internal static MelonPreferences_Entry<bool> _prefDisableKeyBinds;
        internal static MelonPreferences_Entry<float> _prefTimerX;
        internal static MelonPreferences_Entry<float> _prefTimerY;
        internal static MelonPreferences_Entry<bool> _prefHotDogTurtleMode;
        internal static MelonPreferences_Entry<string> _prefMoveKey;
        internal static MelonPreferences_Entry<string> _prefResetKey;
        internal static MelonPreferences_Entry<float> _prefTimerFontSize;
        internal static MelonPreferences_Entry<string> _prefToggleKey;
        private const float DEFAULT_FONT_SIZE = 72f;
        private const float DEFAULT_TIMER_X = 20f;
        private const float DEFAULT_TIMER_Y = -20f;
        private static KeyCode _moveKey;
        private static KeyCode _resetKey;
        private static KeyCode _toggleKey;

        public static void SavePosition(float x, float y)
        {
            _prefTimerX.Value = x;
            _prefTimerY.Value = y;
            MelonPreferences.Save();
        }

        public override void OnInitializeMelon()
        {
            _prefsCategory = MelonPreferences.CreateCategory("IGT_Settings");
            _prefTimerX = _prefsCategory.CreateEntry("TimerPositionX", DEFAULT_TIMER_X, "Timer X Position", description: "X position of the timer. Defaults to 20.");
            _prefTimerY = _prefsCategory.CreateEntry("TimerPositionY", DEFAULT_TIMER_Y, "Timer Y Position", description: "Y position of the timer. Defaults to -20.");
            _prefTimerFontSize = _prefsCategory.CreateEntry("TimerFontSize", DEFAULT_FONT_SIZE, "Timer Font Size", description: "Font size of the timer. Defaults to 72.");
            _prefDisableKeyBinds = _prefsCategory.CreateEntry("DisableKeyBinds", false, "Disable OUAK-IGT KeyBinds", description: "Disable all keybinds, freeing up your function keys once configured.");
            _prefToggleKey = _prefsCategory.CreateEntry("ToggleKey", "F8", "Toggle Overlay Key", description: "The KeyBind for toggling the display of the IGT timer.");
            _prefMoveKey = _prefsCategory.CreateEntry("MoveKey", "F9", "Toggle Move Mode Key", description: "The KeyBind for toggling move mode, allowing the timer to be dragged with mouse left click.");
            _prefResetKey = _prefsCategory.CreateEntry("ResetKey", "F10", "Reset Position Key", description: "The KeyBind to reset the position of the timer back to the default (20, -20).");
            _prefHotDogTurtleMode = _prefsCategory.CreateEntry("HotDogTurtleMode", true, "Hide Milliseconds", description: "HotDogTurtle and the game prefers centiseconds, and you could too. Shows milliseconds when set to false.");

            _toggleKey = ParseKeyCode(_prefToggleKey.Value, KeyCode.F8);
            _moveKey = ParseKeyCode(_prefMoveKey.Value, KeyCode.F9);
            _resetKey = ParseKeyCode(_prefResetKey.Value, KeyCode.F10);

            string keyBindStatus = _prefDisableKeyBinds.Value ? "disabled" : "enabled";
            MelonLogger.Msg($"OUAK-IGT initialized. Keybinds {keyBindStatus}: {_toggleKey}=toggle, {_moveKey}=move, {_resetKey}=reset.");
        }

        /// <summary>
        /// Listen for the configured keybinds and handle move mode.
        /// Skips all execution if <see cref="_prefDisableKeyBinds"/> is false.
        /// </summary>
        public override void OnUpdate()
        {
            if (_prefDisableKeyBinds.Value)
            {
                return;
            }

            if (Input.GetKeyDown(_toggleKey))
            {
                IGTDisplayManager.ShowOverlay = !IGTDisplayManager.ShowOverlay;
                string status = IGTDisplayManager.ShowOverlay ? "SHOWN" : "HIDDEN";
                IGTDisplayManager.ShowNotification($"OVERLAY: {status}");
                MelonLogger.Msg($"[IGT] Overlay {status.ToLower()}");
            }

            if (Input.GetKeyDown(_moveKey))
            {
                IGTDisplayManager.ToggleMoveMode();
            }

            if (Input.GetKeyDown(_resetKey))
            {
                IGTDisplayManager.ResetPosition(DEFAULT_TIMER_X, DEFAULT_TIMER_Y);
                SavePosition(DEFAULT_TIMER_X, DEFAULT_TIMER_Y);
            }

            if (IGTDisplayManager.IsMoveMode)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    IGTDisplayManager.StartDrag(Input.mousePosition);
                }
                else if (Input.GetMouseButton(0))
                {
                    IGTDisplayManager.UpdateDrag(Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    Vector2 newPos = IGTDisplayManager.EndDrag();
                    SavePosition(newPos.x, newPos.y);
                }
            }
        }

        private static KeyCode ParseKeyCode(string keyString, KeyCode fallback)
        {
            if (System.Enum.TryParse(keyString, true, out KeyCode result))
            {
                return result;
            }
            MelonLogger.Warning($"[IGT] Invalid key '{keyString}', using {fallback}");
            return fallback;
        }
    }
}