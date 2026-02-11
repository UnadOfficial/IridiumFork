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
        public bool enableCustomLevelIsland = false;
    }

    public class TailSettings
    {
        public bool enableTailTweak = false;
        public float tailLength = 1f;
        public float tailEmission = 20f;
        public bool tailFollowPitch = false;
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

    public enum SkinMode
    {
        SingleGlobal,
        PerScene,
        Slideshow
    }

    public class SkinConfig
    {
        public string path = "";
        public float scale = 1f;
        public float offsetX = 0f;
        public float offsetY = 0f;
        public float opacity = 1f;
        public float brightness = 1f;
        public float saturation = 1f;
        public float contrast = 1f;
        public float hue = 0f;
        public bool loop = true;
        public float playbackSpeed = 1f;
    }

    public class AppearanceSettings
    {
        public bool enableMenuSkin = false;
        public SkinMode mode = SkinMode.SingleGlobal;
        
        public SkinConfig globalSkin = new();
        public SkinConfig mainUISkin = new();
        public SkinConfig clsSkin = new();
        public SkinConfig dlcUISkin = new();

        public int slideshowCount = 1;
        public float slideDuration = 10f;
        public SkinConfig[] slideshowSkins = [new SkinConfig()];

        // Level Select Track Customization
        public bool enableTrackCustomization = false;
        public bool trackColorR = true;
        public bool trackColorG = true;
        public bool trackColorB = true;
        public Color trackColor = Color.white;
        public float trackOpacity = 1f;
        public float trackBrightness = 1f;

        public void EnsureSlideshowSize()
        {
            if (slideshowCount < 1) slideshowCount = 1;
            if (slideshowSkins == null || slideshowSkins.Length != slideshowCount)
            {
                SkinConfig[] newSkins = new SkinConfig[slideshowCount];
                for (int i = 0; i < slideshowCount; i++)
                {
                    if (slideshowSkins != null && i < slideshowSkins.Length)
                        newSkins[i] = slideshowSkins[i];
                    else
                        newSkins[i] = new SkinConfig();
                }
                slideshowSkins = newSkins;
            }
        }
    }
}
