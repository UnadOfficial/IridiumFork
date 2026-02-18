using UnityEngine;

namespace Iridium.Config
{
    public class OptimizerSettings
    {
        public bool enableOptimizer = false;
        public bool optimizeMoveTrack = false;
        public bool optimizeRecolorTrack = false;
        public bool optimizeFilters = false;
        public double divideBy = 1.0;
        public bool dontShowSavedMemory = false;
        public bool dontCompress = false;
        public bool dontResizeMultipleOf4 = false;
        public bool dontResizeCollider = false;
        public bool disableShadows = false;
        public bool optimizeDecorationUpdate = false;
        public bool optimizeTileUpdate = false;
        public bool fastLoading = false;
        public bool skipEventIfPaused = false;
        public bool optimizeEventIcons = false;
        public bool optimizeScnGameUpdate = false;
        public bool optimizeMoveDecorations = false;
        public bool optimizeFloorMesh = false;
    }

    public class UISettings
    {
        public bool removeNews = false;
        public bool hideBetaWatermark = false;
        public bool moveAutoplayText = false;
        public float autoplayTextX = 0f;
        public float autoplayTextY = 0f;
        public bool forceDifficultyUI = false;
        public bool enableCircleArc = false;
        public bool alwaysCountdown = false;
    }

    public class TailSettings
    {
        public bool enableTailTweak = false;
        public float tailLength = 1f;
        public float tailEmission = 20f;
        public bool tailFollowPitch = false;
    }

    public class LobbyMusicSettings
    {
        public bool enableLobbyMusicPatch = false;
        public bool enableCustomBpm = false;
        public float customBpm = 120f;
        public bool fastMusic = true;
        public bool customMusic = false;
        public string defaultMusicPath = string.Empty;
        public string fastMusicPath = string.Empty;
    }

    public class MemorySettings
    {
        public bool enableMemoryOptimization = false;
        public bool enableSmartGC = false;
        public float gcInterval = 60f;
        public bool gcInGame = false;
        public bool gcInLoadScene = true;
    }

    public class CompatibilitySettings
    {
        public bool enableLegacyPauseFix = false;
        public bool enableNoFailTooEarly = false;
        public bool forceAngleData = false;
        public LegacyBehaviorMode legacyFlashMode = LegacyBehaviorMode.Default;
        public LegacyBehaviorMode legacyCamRelativeToMode = LegacyBehaviorMode.Default;
    }

    public enum LegacyBehaviorMode
    {
        Default,
        AlwaysOff,
        AlwaysOn
    }

    public class HitTextSettings
    {
        public bool enableHitTextCustomization = false;

        // Text settings
        public string tooEarlyText = "";
        public string veryEarlyText = "";
        public string earlyPerfectText = "";
        public string perfectText = "";
        public string latePerfectText = "";
        public string veryLateText = "";
        public string tooLateText = "";
        public string multipressText = "";
        public string overPressText = "";

        // Color settings (nullable to indicate use default)
        public Color? tooEarlyColor = null;
        public Color? veryEarlyColor = null;
        public Color? earlyPerfectColor = null;
        public Color? perfectColor = null;
        public Color? latePerfectColor = null;
        public Color? veryLateColor = null;
        public Color? tooLateColor = null;
        public Color? multipressColor = null;
        public Color? overPressColor = null;

        // Helper methods for serialization (RGBA hex format)
        public string GetColorHex(Color? color)
        {
            if (!color.HasValue) return "";
            var c = color.Value;
            return $"{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}{(int)(c.a * 255):X2}";
        }

        public void SetColorFromHex(ref Color? target, string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                target = null;
                return;
            }
            // Support both RGB (6 chars) and RGBA (8 chars)
            if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int rgb))
            {
                target = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
            }
            else if (hex.Length == 8 && long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out long rgba))
            {
                target = new Color(((rgba >> 24) & 0xFF) / 255f, ((rgba >> 16) & 0xFF) / 255f, ((rgba >> 8) & 0xFF) / 255f, (rgba & 0xFF) / 255f);
            }
            else
            {
                target = null;
            }
        }
    }

    public class AppearanceSettings
    {
    }
}
