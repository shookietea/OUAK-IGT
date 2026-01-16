using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace OUAK_IGT
{
    public static class IGTDisplayManager
    {
        private const float BASE_FONT_SIZE = 72f;
        private const float BASE_TIMER_HEIGHT = 70f;
        private const float BASE_TIMER_WIDTH = 288.7f;

        private const int CANVAS_SORTING_ORDER = 11;

        private const string TIMER_FONT_NAME = "TT_BabyPop-EB SDF";

        private const float NOTIFICATION_FONT_SIZE = 48f;
        private const float NOTIFICATION_HEIGHT = 100f;
        private const float NOTIFICATION_WIDTH = 800f;

        private static readonly Vector2 CENTER_ANCHOR = new Vector2(0.5f, 0.5f);

        private static readonly Vector2 FALLBACK_RESOLUTION = new Vector2(1920, 1080);

        private static readonly Color32 TEXT_COLOR = new Color32(248, 248, 242, 255);
        private static readonly Color32 TEXT_COLOR_MOVE_MODE = new Color32(128, 255, 234, 255);

        private static readonly Vector2 TOP_LEFT_ANCHOR = new Vector2(0f, 1f);

        private static CanvasScaler _cachedGameCanvasScaler;
        private static TMP_FontAsset _cachedGameFont;

        private static GameObject _canvasObject;
        private static RectTransform _canvasRect;
        private static Vector2 _dragStartMousePos;
        private static Vector2 _dragStartTimerPos;

        private static bool _isMoveMode = false;

        private static float _notificationHideTime = 0f;

        private static TextMeshProUGUI _notificationText;

        private static GameObject _timerContainer;

        private static RectTransform _timerContainerRect;

        private static TextMeshProUGUI _timerText;

        private enum TimerColorState
        {
            Normal,
            MoveMode,
            Invalid
        }

        public static TextMeshProUGUI IGTTimerText => _timerText;
        public static bool IsMoveMode => _isMoveMode;
        public static bool ShowOverlay { get; set; } = true;

        /// <summary>
        /// Gets the game's CanvasScaler, caching the result for performance.
        /// Returns null if no suitable CanvasScaler is found.
        /// </summary>
        private static CanvasScaler GameCanvasScaler
        {
            get
            {
                if (_cachedGameCanvasScaler == null)
                {
                    var gameCanvases = GameObject.FindObjectsOfType<Canvas>();
                    foreach (var canvas in gameCanvases)
                    {
                        if (canvas.name == "IGT_Canvas") continue;

                        _cachedGameCanvasScaler = canvas.GetComponent<CanvasScaler>();
                        if (_cachedGameCanvasScaler != null)
                        {
                            MelonLogger.Msg($"[IGT] Found CanvasScaler on: {canvas.name}");
                            break;
                        }
                    }

                    if (_cachedGameCanvasScaler == null)
                    {
                        MelonLogger.Msg("[IGT] No CanvasScaler found, using defaults");
                    }
                }

                return _cachedGameCanvasScaler;
            }
        }

        /// <summary>
        /// Gets the game's font asset, caching the result.
        /// </summary>
        private static TMP_FontAsset GameFont
        {
            get
            {
                if (_cachedGameFont == null)
                {
                    var tmpFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    _cachedGameFont = tmpFonts.FirstOrDefault(f => f != null && f.name == TIMER_FONT_NAME);

                    if (_cachedGameFont == null)
                    {
                        _cachedGameFont = tmpFonts.FirstOrDefault(f => f != null);
                        MelonLogger.Error($"[IGT] We rolled up a strange font: {_cachedGameFont.name}");
                        ShowNotification("Font not found! Please report this bug to the Speedrun Discord.", 20);
                    }

                    MelonLogger.Msg($"[IGT] Using font: {_cachedGameFont.name}");
                }

                return _cachedGameFont;
            }
        }

        /// <summary>
        /// Gets the reference resolution for UI scaling and boundary calculations.
        /// Returns the game's CanvasScaler resolution if available, otherwise falls back to 1920x1080.
        /// </summary>
        private static Vector2 ReferenceResolution =>
            GameCanvasScaler?.referenceResolution ?? FALLBACK_RESOLUTION;

        /// <summary>
        /// Clears cached references that may become stale when resolution changes.
        /// Called when MainGameManager is destroyed.
        /// </summary>
        public static void ClearCachedReferences()
        {
            _cachedGameCanvasScaler = null;
            _cachedGameFont = null;
            MelonLogger.Msg("[IGT] Cleared cached references (resolution may have changed)");
        }

        /// <summary>
        /// Creates the display canvas, setting up timer container, timer text, and notifications.
        /// Returns the position used (may be clamped if input was out of bounds).
        /// </summary>
        public static Vector2 CreateDisplay(float timerX, float timerY)
        {
            if (_timerContainer != null)
            {
                MelonLogger.Msg("[IGT] IGT display already exists");
                return Vector2.zero;
            }

            try
            {
                MelonLogger.Msg("[IGT] === Creating IGT Display Manager ===");

                // Create canvas (uses GameCanvasScaler property which auto-caches)
                (_canvasObject, var scaler) = CreateCanvas();
                _canvasRect = _canvasObject.GetComponent<RectTransform>();
                MelonLogger.Msg("[IGT] Canvas created and set to DontDestroyOnLoad");

                // Find game font (uses GameFont property which auto-caches)
                if (GameFont == null)
                {
                    MelonLogger.Error("[IGT] Could not find game font!");
                    return Vector2.zero;
                }

                // Clamp timer position to screen boundaries
                Vector2 timerDimensions = GetTimerDimensions();
                Vector2 clampedPosition = ClampTimerPosition(timerX, timerY, scaler.referenceResolution, timerDimensions);

                // Create UI elements
                MelonLogger.Msg("[IGT] === Creating Timer Container ===");
                (_timerContainer, _timerText) = CreateTimerUI(
                    _canvasObject.transform,
                    GameFont,
                    IGT._prefTimerFontSize.Value,
                    clampedPosition,
                    timerDimensions);
                _timerContainerRect = _timerContainer.GetComponent<RectTransform>();
                MelonLogger.Msg($"[IGT] Timer positioned at: ({clampedPosition.x}, {clampedPosition.y})");

                _notificationText = CreateNotificationUI(_canvasObject.transform, GameFont);

                // Activate
                _canvasObject.SetActive(true);
                _timerContainer.SetActive(true);

                MelonLogger.Msg("[IGT] === Display Created ===");
                MelonLogger.Msg($"[IGT] Canvas active: {_canvasObject.activeSelf}");
                MelonLogger.Msg($"[IGT] Timer active: {_timerContainer.activeSelf}");

                return clampedPosition;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[IGT] Error creating display: {e}");
                return Vector2.zero;
            }
        }

        /// <summary>
        /// Ends the drag operation and returns the final timer position for saving.
        /// </summary>
        public static Vector2 EndDrag()
        {
            if (_timerContainerRect == null) return Vector2.zero;

            Vector2 finalPos = _timerContainerRect.anchoredPosition;
            MelonLogger.Msg($"[IGT] Drag ended at position: ({finalPos.x}, {finalPos.y})");
            return finalPos;
        }

        /// <summary>
        /// Resets timer position to the specified coordinates.
        /// </summary>
        public static void ResetPosition(float x, float y)
        {
            if (_timerContainerRect == null)
            {
                MelonLogger.Warning("[IGT] Cannot reset position - timer container not initialized");
                return;
            }

            // Clamp to ensure position is valid
            Vector2 clampedPos = ClampTimerPosition(x, y, ReferenceResolution, GetTimerDimensions());

            _timerContainerRect.anchoredPosition = clampedPos;
            ShowNotification("POSITION RESET");
            MelonLogger.Msg($"[IGT] Position reset to: ({clampedPos.x}, {clampedPos.y})");
        }

        /// <summary>
        /// Sets whether the current run is invalid for IL leaderboards.
        /// Invalid runs display the timer in coral red color.
        /// </summary>
        /// <param name="invalid">True if run is invalid, false if valid.</param>
        public static void SetRunInvalid(bool invalid)
        {
            throw new NotImplementedException("SetRunInvalid will be implemented in the future, maybe.");
        }

        /// <summary>
        /// Directly sets timer visibility (used when MainGameManager is destroyed).
        /// </summary>
        public static void SetVisibility(bool visible)
        {
            if (_timerText != null)
            {
                _timerText.enabled = visible;
            }
        }

        /// <summary>
        /// Shows a centered notification message that auto-hides after the specified duration.
        /// </summary>
        public static void ShowNotification(string message, float duration = 2f)
        {
            if (_notificationText == null) return;

            _notificationText.text = message;
            _notificationText.enabled = true;
            _notificationHideTime = Time.time + duration;
        }

        /// <summary>
        /// Begins dragging the timer from the current mouse position.
        /// </summary>
        public static void StartDrag(Vector3 mousePos)
        {
            if (!_isMoveMode || _timerContainerRect == null || _canvasRect == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                mousePos,
                null,
                out localPoint);

            _dragStartMousePos = localPoint;
            _dragStartTimerPos = _timerContainerRect.anchoredPosition;
        }

        /// <summary>
        /// Toggles move mode on/off, allowing the user to drag the timer to a new position.
        /// </summary>
        public static void ToggleMoveMode()
        {
            _isMoveMode = !_isMoveMode;
            if (_isMoveMode)
            {
                SetTimerColorState(TimerColorState.MoveMode);
                ShowNotification("MOVE MODE: ON");
                MelonLogger.Msg("[IGT] Move mode enabled");
            }
            else
            {
                SetTimerColorState(TimerColorState.Normal);
                ShowNotification("MOVE MODE: OFF");
                MelonLogger.Msg("[IGT] Move mode disabled");
            }
        }

        /// <summary>
        /// Updates the timer display with current playtime and manages overlay visibility.
        /// </summary>
        public static void UpdateDisplay(float playTime)
        {
            if (_timerText == null) return;

            string formattedTime = FormatSeconds(playTime);
            _timerText.text = formattedTime;

            if (_timerText.enabled != ShowOverlay)
            {
                _timerText.enabled = ShowOverlay;
            }

            UpdateNotifications();
        }

        /// <summary>
        /// Updates timer position during drag, clamping to screen boundaries.
        /// </summary>
        public static void UpdateDrag(Vector3 mousePos)
        {
            if (!_isMoveMode || _timerContainerRect == null || _canvasRect == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                mousePos,
                null,
                out localPoint);

            Vector2 delta = localPoint - _dragStartMousePos;
            Vector2 newPos = _dragStartTimerPos + delta;

            // Get current resolution and timer dimensions for clamping
            Vector2 referenceResolution = ReferenceResolution;
            Vector2 timerDimensions = GetTimerDimensions();

            newPos.x = Mathf.Clamp(newPos.x, 0f, referenceResolution.x - timerDimensions.x);
            newPos.y = Mathf.Clamp(newPos.y, -(referenceResolution.y - timerDimensions.y), 0f);

            _timerContainerRect.anchoredPosition = newPos;
        }

        /// <summary>
        /// Applies outline and shadow effects to TextMeshPro font material for visibility.
        /// </summary>
        private static void ApplyTextOutlineAndShadow(Material material)
        {
            material.EnableKeyword("OUTLINE_ON");
            material.EnableKeyword("UNDERLAY_ON");
            material.SetFloat("_OutlineWidth", 0.3f);
            material.SetColor("_OutlineColor", Color.black);
            material.SetFloat("_FaceDilate", 0.3f);
            material.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.5f));
            material.SetFloat("_UnderlayOffsetX", 0.5f);
            material.SetFloat("_UnderlayOffsetY", -0.5f);
            material.SetFloat("_UnderlayDilate", 0.3f);
            material.SetFloat("_UnderlaySoftness", 0.1f);
        }

        private static Vector2 ClampTimerPosition(float x, float y, Vector2 resolution, Vector2 dimensions)
        {
            float originalX = x;
            float originalY = y;

            x = Mathf.Clamp(x, 0f, resolution.x - dimensions.x);
            y = Mathf.Clamp(y, -(resolution.y - dimensions.y), 0f);

            if (originalX != x || originalY != y)
            {
                MelonLogger.Warning($"[IGT] Timer out of bounds! Clamped from ({originalX}, {originalY}) to ({x}, {y})");
            }

            return new Vector2(x, y);
        }

        /// <summary>
        /// Configures a RectTransform with the specified anchors, pivot, position, and size.
        /// Reusable for all UI elements (timer, notifications, future: input display, dash meter, etc.)
        /// </summary>
        private static void ConfigureRectTransform(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// Configures a TextMeshProUGUI component for timer display with outline and shadow effects.
        /// </summary>
        private static void ConfigureTimerText(
            TextMeshProUGUI text,
            TMP_FontAsset font,
            float fontSize,
            Vector2 dimensions)
        {
            text.font = font;
            text.fontSize = fontSize;
            text.color = TEXT_COLOR;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.text = IGT._prefHotDogTurtleMode.Value ? "00:00.00" : "00:00.000";

            var rect = text.GetComponent<RectTransform>();
            ConfigureRectTransform(rect,
                TOP_LEFT_ANCHOR, TOP_LEFT_ANCHOR, TOP_LEFT_ANCHOR,
                Vector2.zero, dimensions);

            ApplyTextOutlineAndShadow(text.fontMaterial);
            text.UpdateVertexData();
            text.UpdateMeshPadding();
        }

        /// <summary>
        /// Creates the IGT canvas with CanvasScaler and GraphicRaycaster.
        /// Returns a tuple containing the canvas GameObject and its CanvasScaler component.
        /// </summary>
        private static (GameObject canvasObject, CanvasScaler scaler) CreateCanvas()
        {
            var canvasObj = new GameObject("IGT_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CANVAS_SORTING_ORDER;

            var scaler = canvasObj.AddComponent<CanvasScaler>();

            if (GameCanvasScaler != null)
            {
                scaler.uiScaleMode = GameCanvasScaler.uiScaleMode;
                scaler.referenceResolution = GameCanvasScaler.referenceResolution;
                scaler.screenMatchMode = GameCanvasScaler.screenMatchMode;
                scaler.matchWidthOrHeight = GameCanvasScaler.matchWidthOrHeight;
                MelonLogger.Msg("[IGT] CanvasScaler copied from game");
            }
            else
            {
                // Use fallback defaults
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                // Note: screenMatchMode and matchWidthOrHeight use Unity's defaults
                MelonLogger.Msg("[IGT] No CanvasScaler found, using 1080p default");
            }

            canvasObj.AddComponent<GraphicRaycaster>();
            GameObject.DontDestroyOnLoad(canvasObj);

            return (canvasObj, scaler);
        }

        /// <summary>
        /// Creates the notification text UI component centered on screen.
        /// Returns the TextMeshProUGUI component for notifications.
        /// </summary>
        private static TextMeshProUGUI CreateNotificationUI(Transform parent, TMP_FontAsset font)
        {
            var notificationObj = new GameObject("IGT_Notification");
            notificationObj.transform.SetParent(parent, false);
            var text = notificationObj.AddComponent<TextMeshProUGUI>();

            text.font = font;
            text.fontSize = NOTIFICATION_FONT_SIZE;
            text.color = TEXT_COLOR;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.text = "";
            text.enabled = false;

            var rect = notificationObj.GetComponent<RectTransform>();
            ConfigureRectTransform(rect,
                CENTER_ANCHOR, CENTER_ANCHOR, CENTER_ANCHOR,
                Vector2.zero, new Vector2(NOTIFICATION_WIDTH, NOTIFICATION_HEIGHT));

            ApplyTextOutlineAndShadow(text.fontMaterial);
            text.UpdateVertexData();
            text.UpdateMeshPadding();

            return text;
        }

        /// <summary>
        /// Creates the timer UI container and text component.
        /// Returns a tuple containing the timer container GameObject and TextMeshProUGUI component.
        /// </summary>
        private static (GameObject container, TextMeshProUGUI text) CreateTimerUI(
            Transform parent,
            TMP_FontAsset font,
            float fontSize,
            Vector2 position,
            Vector2 dimensions)
        {
            // Container
            var container = new GameObject("IGT_Timer");
            container.transform.SetParent(parent, false);
            var containerRect = container.AddComponent<RectTransform>();
            ConfigureRectTransform(containerRect,
                TOP_LEFT_ANCHOR, TOP_LEFT_ANCHOR, TOP_LEFT_ANCHOR,
                position, dimensions);

            // Text
            var textObj = new GameObject("TimerText");
            textObj.transform.SetParent(container.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            ConfigureTimerText(text, font, fontSize, dimensions);

            return (container, text);
        }

        private static string FormatSeconds(float seconds)
        {
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);

            if (IGT._prefHotDogTurtleMode.Value)
            {
                int centiseconds = ts.Milliseconds / 10;
                return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}.{centiseconds:00}";
            }
            else
            {
                return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
            }
        }

        /// <summary>
        /// Calculates timer dimensions based on configured font size and HotDogTurtleMode.
        /// Base dimensions (288.7x70) tested for fontSize 72 with centisecond display.
        /// Adds extra width when milliseconds are shown (HotDogTurtleMode disabled).
        /// </summary>
        private static Vector2 GetTimerDimensions()
        {
            float fontSize = IGT._prefTimerFontSize.Value;
            float scale = fontSize / BASE_FONT_SIZE;

            float width = BASE_TIMER_WIDTH;

            // Add extra width when showing milliseconds
            if (!IGT._prefHotDogTurtleMode.Value)
            {
                width += 37f;
            }

            return new Vector2(width * scale, BASE_TIMER_HEIGHT * scale);
        }

        /// <summary>
        /// Sets the timer text color based on the current timer state.
        /// </summary>
        /// <param name="state">The color state to apply (Normal, MoveMode, or Invalid).</param>
        private static void SetTimerColorState(TimerColorState state)
        {
            if (_timerText == null) return;

            Color32 newColor = state switch
            {
                TimerColorState.MoveMode => TEXT_COLOR_MOVE_MODE,
                _ => TEXT_COLOR
            };

            _timerText.color = newColor;
            MelonLogger.Msg($"[IGT] Timer color changed to: {state}");
        }

        private static void UpdateNotifications()
        {
            if (_notificationText == null) return;

            if (_notificationText.enabled && Time.time >= _notificationHideTime)
            {
                _notificationText.enabled = false;
                _notificationText.text = "";
            }
        }
    }
}