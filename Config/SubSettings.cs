using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Iridium.Config
{
    public class OptimizerSettings
    {
        public bool enableOptimizer = false;
        public bool optimizeMoveTrack = false;
        public bool optimizeRecolorTrack = false;
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
        public bool enableSmartGC = false;
        public float gcInterval = 60f;
        public bool gcInGame = false;
        public bool gcInLoadScene = false;
    }

    public class CompatibilitySettings
    {
        public bool enableLegacyPauseFix = false;
        public bool enableNoFailTooEarly = false;
        public bool forceAngleData = false;
        public LegacyBehaviorMode legacyFlashMode = LegacyBehaviorMode.Default;
        public LegacyBehaviorMode legacyCamRelativeToMode = LegacyBehaviorMode.Default;
    }

    public class AppearanceSettings
    {
        public bool enableMenuSkin = false;
        
        // Playlist Management
        public string activePlaylistId = "";
        public List<string> playlistOrder = new();
        public bool needsMigration = false;

        // Legacy Settings (Keep for migration)
        public string skinPath = ""; 
        public float backgroundOpacity = 1f;
        public Color backgroundColor = Color.white;
        public float backgroundBlur = 0f;
        public float backgroundBrightness = 1f;
        public float backgroundSaturation = 1f;
        public float backgroundContrast = 1f;
        public float backgroundHue = 0f;
        public bool backgroundLoop = true;
        public float backgroundPlaybackSpeed = 1f;
        public bool backgroundAudio = false;
        public float backgroundAudioVolume = 1f;
        public bool useParallax = false;
        public float parallaxStrength = 0.1f;

        // Level Select Track Customization
        public bool enableTrackCustomization = false;
        public Color trackColor = Color.white;
        public float trackOpacity = 1f;
        public float trackBrightness = 1f;
    }

    public enum LegacyBehaviorMode
    {
        Default,
        AlwaysOff,
        AlwaysOn
    }
}
