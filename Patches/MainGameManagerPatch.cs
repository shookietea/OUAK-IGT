using HarmonyLib;
using Il2CppApp.KatamariSin;
using MelonLoader;

namespace OUAK_IGT.Patches
{
    /// <summary>
    /// Double check the captured <see cref="MainGameManager"/> is the one being destroyed.
    /// Then clear some state and hide the overlay.
    /// </summary>
    [HarmonyPatch(typeof(MainGameManager), "OnDestroy")]
    public class MainGameManagerDestroyPatch
    {
        private static void Prefix(MainGameManager __instance)
        {
            if (__instance != null && __instance == MainGameManagerPatch._mainGameManager)
            {
                MelonLogger.Msg("[IGT] === MainGameManager DESTROYED ===");
                MelonLogger.Msg($"[IGT]    Final PlayTime: {__instance.PlayTime}");
                MelonLogger.Msg($"[IGT]    Final StartTime: {__instance.StartTime}");
                MelonLogger.Msg($"[IGT]    Final NowTime: {__instance.NowTime}");

                IGTDisplayManager.SetVisibility(false);
                MelonLogger.Msg("[IGT] Timer hidden");

                IGTDisplayManager.ClearCachedReferences();

                MainGameManagerPatch._mainGameManager = null;
                MelonLogger.Msg("[IGT] Cleared MainGameManager reference");
            }
            else if (__instance != null && __instance != MainGameManagerPatch._mainGameManager)
            {
                MelonLogger.Warning("[IGT] Unexpected MainGameManager destroyed");
            }
        }
    }

    /// <summary>
    /// Hook into <see cref="MainGameManager.PlayTime"/> for the in-game time display.
    /// PlayTime is an incrementing count of the seconds elapsed since starting the level.
    /// The PlayTime pauses during pause screens, loading screens, and only starts once the level officially begins.
    /// "Officially begins" is determined on the type of level. May be on first object pickup or first movement.
    /// </summary>
    [HarmonyPatch(typeof(MainGameManager), "Update")]
    public class MainGameManagerPatch
    {
        internal static MainGameManager _mainGameManager;

        private static void Postfix()
        {
            if (_mainGameManager != null && IGTDisplayManager.IGTTimerText != null)
            {
                IGTDisplayManager.UpdateDisplay(_mainGameManager.PlayTime);
            }
        }

        private static void Prefix(MainGameManager __instance)
        {
            if (__instance != null && _mainGameManager == null)
            {
                _mainGameManager = __instance;

                float originalX = IGT._prefTimerX.Value;
                float originalY = IGT._prefTimerY.Value;
                UnityEngine.Vector2 actualPos = IGTDisplayManager.CreateDisplay(originalX, originalY);

                // Save corrected position if clamping occurred (ensures preferences stay valid)
                if (actualPos.x != originalX || actualPos.y != originalY)
                {
                    MelonLogger.Msg("[IGT] Saving corrected position to preferences");
                    IGT.SavePosition(actualPos.x, actualPos.y);
                }
            }
        }
    }
}