using System;
using UnityModManagerNet;
using UnityEngine;
using Iridium.UI;
using Iridium.Config;

namespace Iridium
{
    public class Settings : UnityModManager.ModSettings
    {
        public string language = "en";
        
        public OptimizerSettings optimizer = new();
        public UISettings ui = new();
        public TailSettings tail = new();
        public MemorySettings memory = new();
        public CompatibilitySettings compatibility = new();

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UIUtils.InitializeStyles();

            GUILayout.BeginHorizontal();

            // --- Left Column ---
            GUILayout.BeginVertical(GUILayout.Width(380)); // Scaled from 420

            // Language Selection Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("Language"), UIUtils.HeaderStyle);
            
            GUILayout.BeginHorizontal();
            var langs = Localization.AvailableLanguages;
            foreach (var lang in langs)
            {
                bool isCurrent = language == lang;
                if (isCurrent) GUI.color = new(0.81f, 0.88f, 1.0f);
                string displayName = Localization.GetDisplayName(lang);
                if (GUILayout.Button(displayName.ToUpper(), UIUtils.ButtonStyle, GUILayout.Width(90)))
                {
                    language = lang;
                }
                GUI.color = Color.white;
                GUILayout.Space(5);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // CompressDecorations Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("EnableOptimizer"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            bool newEnableOptimizer = UIUtils.M3Switch(optimizer.enableOptimizer, "");
            if (newEnableOptimizer != optimizer.enableOptimizer)
            {
                optimizer.enableOptimizer = newEnableOptimizer;
                if (optimizer.enableOptimizer && optimizer.disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
                else QualitySettings.shadows = ShadowQuality.All;
            }
            GUILayout.EndHorizontal();

            if (optimizer.enableOptimizer)
            {
                GUILayout.Space(6);
                
                if (Iridium.Patches.OptimizerPatches.savedVRAM_MB > 0.1f)
                {
                    UIUtils.DrawInfoBox("✨ " + Localization.Get("SavedMemoryMsg", Iridium.Patches.OptimizerPatches.savedVRAM_MB.ToString("F2")));
                    GUILayout.Space(6);
                }

                bool showSavedMemory = UIUtils.M3Switch(!optimizer.dontShowSavedMemory, Localization.Get("ShowSavedMemory"));
                if (showSavedMemory == optimizer.dontShowSavedMemory)
                {
                    optimizer.dontShowSavedMemory = !showSavedMemory;
                }

                bool compressImage = UIUtils.M3Switch(!optimizer.dontCompress, Localization.Get("CompressImage"));
                if (compressImage == optimizer.dontCompress)
                {
                    optimizer.dontCompress = !compressImage;
                }

                bool multipleOf4 = UIUtils.M3Switch(!optimizer.dontResizeMultipleOf4, Localization.Get("MultipleOf4"));
                if (multipleOf4 == optimizer.dontResizeMultipleOf4)
                {
                    optimizer.dontResizeMultipleOf4 = !multipleOf4;
                }
                
                if (optimizer.dontCompress) optimizer.dontResizeMultipleOf4 = true;

                GUILayout.Space(6);
                GUILayout.BeginHorizontal(GUILayout.Height(24));
                GUILayout.Label(Localization.Get("DivideImageBy"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                string divideByStr = GUILayout.TextField(optimizer.divideBy.ToString("F1"), UIUtils.TextFieldStyle, GUILayout.Width(45));
                if (double.TryParse(divideByStr, out double newDivideBy)) optimizer.divideBy = newDivideBy;
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                optimizer.dontResizeCollider = UIUtils.M3Switch(optimizer.dontResizeCollider, Localization.Get("DontResizeCollider"));
                
                bool newDisableShadows = UIUtils.M3Switch(optimizer.disableShadows, Localization.Get("DisableShadows"));
                if (newDisableShadows != optimizer.disableShadows)
                {
                    optimizer.disableShadows = newDisableShadows;
                    if (optimizer.enableOptimizer && optimizer.disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
                    else QualitySettings.shadows = ShadowQuality.All;
                }

                optimizer.optimizeDecorationUpdate = UIUtils.M3Switch(optimizer.optimizeDecorationUpdate, Localization.Get("OptimizeDecorationUpdate"));
                optimizer.optimizeTileUpdate = UIUtils.M3Switch(optimizer.optimizeTileUpdate, Localization.Get("OptimizeTileUpdate"));
                optimizer.optimizeMoveTrack = UIUtils.M3Switch(optimizer.optimizeMoveTrack, Localization.Get("OptimizeMoveTrack"));
                optimizer.optimizeRecolorTrack = UIUtils.M3Switch(optimizer.optimizeRecolorTrack, Localization.Get("OptimizeRecolorTrack"));
                optimizer.fastLoading = UIUtils.M3Switch(optimizer.fastLoading, Localization.Get("FastLoading"));

                // Error states
                if (typeof(Notification).GetMethod("SetupNotification", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) == null)
                {
                    GUILayout.Space(4);
                    UIUtils.DrawInfoBox("⚠ " + Localization.Get("MethodNotFound", "Notification.SetupNotification"), true);
                    optimizer.dontShowSavedMemory = true;
                }
                if (typeof(scrVisualDecoration).GetProperty("spriteUnscaledSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) == null)
                {
                    GUILayout.Space(4);
                    UIUtils.DrawInfoBox("⚠ " + Localization.Get("PropertyNotFound", "scrVisualDecoration.spriteUnscaledSize"), true);
                    optimizer.dontResizeCollider = true;
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // Memory Optimization Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("MemorySettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            memory.enableSmartGC = UIUtils.M3Switch(memory.enableSmartGC, "");
            GUILayout.EndHorizontal();

            if (memory.enableSmartGC)
            {
                GUILayout.Space(6);
                GUILayout.BeginHorizontal(GUILayout.Height(24));
                GUILayout.Label(Localization.Get("GCInterval"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                string intervalStr = GUILayout.TextField(memory.gcInterval.ToString("F0"), UIUtils.TextFieldStyle, GUILayout.Width(45));
                if (float.TryParse(intervalStr, out float newInterval)) memory.gcInterval = Mathf.Clamp(newInterval, 10f, 3600f);
                GUILayout.EndHorizontal();

                memory.gcInGame = UIUtils.M3Switch(memory.gcInGame, Localization.Get("GCInGame"));
                memory.gcInLoadScene = UIUtils.M3Switch(memory.gcInLoadScene, Localization.Get("GCInLoadScene"));
            }
            GUILayout.EndVertical();

            GUILayout.EndVertical(); // End Left Column

            GUILayout.Space(14);

            // --- Right Column ---
            GUILayout.BeginVertical(GUILayout.Width(380));

            // UI Adjustments Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("UISettings"), UIUtils.HeaderStyle);
            GUILayout.Space(6);
            bool newRemoveNews = UIUtils.M3Switch(ui.removeNews, Localization.Get("RemoveNews"));
            if (newRemoveNews != ui.removeNews)
            {
                ui.removeNews = newRemoveNews;
                Iridium.Patches.MiscPatches.RemoveNewsPatch.UpdateNews();
            }
            bool newHideBeta = UIUtils.M3Switch(ui.hideBetaWatermark, Localization.Get("HideBetaWatermark"));
            if (newHideBeta != ui.hideBetaWatermark)
            {
                ui.hideBetaWatermark = newHideBeta;
                Iridium.Patches.MiscPatches.RefreshBetaWatermark();
            }
            ui.forceDifficultyUI = UIUtils.M3Switch(ui.forceDifficultyUI, Localization.Get("ForceDifficultyUI"));

            bool newMoveAutoplay = UIUtils.M3Switch(ui.moveAutoplayText, Localization.Get("MoveAutoplayText"));
            if (newMoveAutoplay != ui.moveAutoplayText)
            {
                ui.moveAutoplayText = newMoveAutoplay;
                Iridium.Patches.MiscPatches.RefreshAutoplayTextPosition();
            }

            if (ui.moveAutoplayText)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(18));
                ui.autoplayTextX = GUILayout.HorizontalSlider(ui.autoplayTextX, -Screen.width / 2f, Screen.width / 2f);
                GUILayout.Label(ui.autoplayTextX.ToString("F0"), UIUtils.LabelStyle, GUILayout.Width(36));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(18));
                ui.autoplayTextY = GUILayout.HorizontalSlider(ui.autoplayTextY, -Screen.height / 2f, Screen.height / 2f);
                GUILayout.Label(ui.autoplayTextY.ToString("F0"), UIUtils.LabelStyle, GUILayout.Width(36));
                GUILayout.EndHorizontal();

                if (GUI.changed)
                {
                    Iridium.Patches.MiscPatches.RefreshAutoplayTextPosition();
                }
            }
            
            GUILayout.Space(6);
            ui.enableCircleArc = UIUtils.M3Switch(ui.enableCircleArc, Localization.Get("EnableCircleArc"));
            if (ui.enableCircleArc) UIUtils.DrawInfoBox("⚠ " + Localization.Get("RestartRequired"));
            
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // Tail Settings Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("TailSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            bool newEnableTail = UIUtils.M3Switch(tail.enableTailTweak, "");
            if (newEnableTail != tail.enableTailTweak)
            {
                tail.enableTailTweak = newEnableTail;
                if (!tail.enableTailTweak) Iridium.Patches.MiscPatches.TailTweakPatch.ResetTails();
            }
            GUILayout.EndHorizontal();

            if (tail.enableTailTweak)
            {
                GUILayout.Space(6);
                bool newFollowPitch = UIUtils.M3Switch(tail.tailFollowPitch, Localization.Get("TailFollowPitch"));
                if (newFollowPitch != tail.tailFollowPitch)
                {
                    tail.tailFollowPitch = newFollowPitch;
                    if (!tail.tailFollowPitch) Iridium.Patches.MiscPatches.TailTweakPatch.ResetTails();
                }
                
                if (!tail.tailFollowPitch)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(24));
                    GUILayout.Label(Localization.Get("TailLength"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string lengthStr = GUILayout.TextField(tail.tailLength.ToString("F1"), UIUtils.TextFieldStyle, GUILayout.Width(45));
                    if (float.TryParse(lengthStr, out float newLength)) tail.tailLength = newLength;
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal(GUILayout.Height(24));
                GUILayout.Label(Localization.Get("TailEmission"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                string emissionStr = GUILayout.TextField(tail.tailEmission.ToString("F1"), UIUtils.TextFieldStyle, GUILayout.Width(45));
                if (float.TryParse(emissionStr, out float newEmission)) tail.tailEmission = newEmission;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // Compatibility & Fixes Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("CompatibilitySettings"), UIUtils.HeaderStyle);
            GUILayout.Space(6);
            compatibility.enableLegacyPauseFix = UIUtils.M3Switch(compatibility.enableLegacyPauseFix, Localization.Get("EnableLegacyPauseFix"));
            compatibility.enableNoFailTooEarly = UIUtils.M3Switch(compatibility.enableNoFailTooEarly, Localization.Get("EnableNoFailTooEarly"));
            
            GUILayout.Space(10);
            GUILayout.Label(Localization.Get("LegacyLevelBehavior"), UIUtils.LabelStyle, GUILayout.Height(22));
            
            compatibility.forceAngleData = UIUtils.M3Switch(compatibility.forceAngleData, Localization.Get("ForceAngleData"));

            GUILayout.Space(4);
            
            GUIStyle subContainerStyle = new()
            {
                normal = { background = UIUtils.GetCachedRoundedTex(64, 64, 6, new Color(1, 1, 1, 0.03f)) }, 
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 4, 4)
            };
            GUILayout.BeginVertical(subContainerStyle); // Sub-container
            
            GUILayout.Label(Localization.Get("LegacyFlashMode"), UIUtils.LabelStyle);
            GUILayout.Space(2);
            compatibility.legacyFlashMode = (LegacyBehaviorMode)UIUtils.M3SegmentedButton((int)compatibility.legacyFlashMode, 
                [Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn")]);
            
            GUILayout.Space(8);
            GUILayout.Label(Localization.Get("LegacyCamRelativeToMode"), UIUtils.LabelStyle);
            GUILayout.Space(2);
            compatibility.legacyCamRelativeToMode = (LegacyBehaviorMode)UIUtils.M3SegmentedButton((int)compatibility.legacyCamRelativeToMode, 
                [Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn")]);
            
            GUILayout.EndVertical();
            
            GUILayout.EndVertical();

            GUILayout.EndVertical(); // End Right Column

            GUILayout.EndHorizontal();

            if (GUI.changed)
            {            
                Save(modEntry);
            }
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
