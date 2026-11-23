using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Pathologic2SmoothStrayAnimations
{
    // Stray ~30 FPS animations. Pathologic 2 runs physics at a fixed 0.033 s step (~30 Hz). Some staged characters (the girl playing hopscotch in
    // the prologue, soldier and orderly dummies, dancing Herb Brides, the bull, cutscene actors) have their Animator
    // set to AnimatorUpdateMode.AnimatePhysics in the game assets, so their pose changes only on physics steps and
    // looks like ~30 FPS at any frame rate. The mod switches such animators to Normal (evaluated every frame).
    // See SceneAnimators for which animators are switched and how.
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "pathologic2.smoothstrayanimations";
        public const string Name = "Pathologic 2 Smooth Stray Animations";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<KeyboardShortcut> ToggleKey;
        internal static ConfigEntry<bool> ShowMessages;
        internal static ConfigEntry<bool> InterpolateBodies;
        internal static ConfigEntry<string> SkipControllers;
        internal static ConfigEntry<bool> LogDetails;

        private static string osdText;
        private static float osdUntil;
        private static GUIStyle osdStyle;
        private bool firstScanDone;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Switch staged characters whose animation runs at the physics rate (~30 Hz) to per-frame animation. Toggled in game with ToggleKey.");
            ToggleKey = Config.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.Insert),
                "Key that toggles Enabled in game to compare with vanilla. The game itself does not use Insert.");
            ShowMessages = Config.Bind("General", "ShowMessages", true, "Show an on-screen message when toggling.");
            InterpolateBodies = Config.Bind("Fixes", "InterpolateBodies", true,
                "If a switched character also has a non-kinematic Rigidbody (the burning Herb Bride, one boy with a shovel), " +
                "turn on Rigidbody interpolation so its position also moves every frame.");
            SkipControllers = Config.Bind("Fixes", "SkipControllers",
                "Tragedian_Pose_Finger_Left_Animator, Tragedian_Pose_Finger_Right_Animator, Bull_Dead_Animator",
                "Comma-separated animator controller names that are left as they are. The defaults are frozen poses " +
                "(all their animation curves are constant), so per-frame updates would cost CPU time without any visible change.");
            LogDetails = Config.Bind("Diagnostics", "Log", false,
                "Write to BepInEx\\LogOutput.log every switched animator, the time spent on each scene load and new prefab, " +
                "and once a minute the cost of the spawn hook.");

            try
            {
                new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
                SceneAnimators.Init();
            }
            catch (Exception e)
            {
                Log.LogError("Failed to apply patches, mod disabled: " + e);
                enabled = false;
                return;
            }
            Log.LogInfo($"{Name} {Version}: Enabled={Enabled.Value}, InterpolateBodies={InterpolateBodies.Value}, " +
                        $"ToggleKey={ToggleKey.Value}, Log={LogDetails.Value}, fixedDeltaTime={Time.fixedDeltaTime:0.####} s");
        }

        private void Update()
        {
            if (!firstScanDone)
            {
                // Scenes loaded before the plugin started are not reported by sceneLoaded.
                firstScanDone = true;
                if (Enabled.Value)
                {
                    SceneAnimators.ScanLoaded("startup");
                }
            }
            if (IsPressed(ToggleKey.Value))
            {
                Enabled.Value = !Enabled.Value;
                int count = SceneAnimators.SetEnabled(Enabled.Value);
                string text = Enabled.Value
                    ? $"Smooth Stray Animations: on ({count} animators per frame)"
                    : "Smooth Stray Animations: off (vanilla ~30 Hz)";
                Log.LogInfo(text + "; " + SceneAnimators.Stats());
                ShowMessage(text);
            }
            if (LogDetails.Value)
            {
                SceneAnimators.LogHookStats();
            }
        }

        // BepInEx's KeyboardShortcut.IsDown() does not fire while any unrelated key is held (WASD, mouse buttons).
        // Here unrelated keys do not interfere: only the main key and the modifiers listed in the shortcut matter.
        private static bool IsPressed(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None || !Input.GetKeyDown(shortcut.MainKey))
            {
                return false;
            }
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return false;
                }
            }
            return true;
        }

        private void OnGUI()
        {
            if (osdText == null || Time.unscaledTime > osdUntil || !ShowMessages.Value)
            {
                return;
            }
            GUI.depth = -1000;
            if (osdStyle == null)
            {
                osdStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            }
            var rect = new Rect(24, 144, 900, 40);
            var shadow = new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height);
            osdStyle.normal.textColor = Color.black;
            GUI.Label(shadow, osdText, osdStyle);
            osdStyle.normal.textColor = Color.white;
            GUI.Label(rect, osdText, osdStyle);
        }

        private static void ShowMessage(string text)
        {
            osdText = text;
            osdUntil = Time.unscaledTime + 2.5f;
        }
    }
}
