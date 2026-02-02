namespace Iridium.Config
{
    public class OptimizerSettings
    {
        public bool enableOptimizer = false;
        public double divideBy = 1.0;
        public bool dontShowSavedMemory = false;
        public bool dontCompress = false;
        public bool dontResizeMultipleOf4 = false;
        public bool dontResizeCollider = false;
        public bool disableShadows = false;
        public bool optimizeDecorationUpdate = true;
        public bool optimizeTileUpdate = true;
        public bool fastLoading = true;
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
        public float tailEmission = 1f;
        public bool tailFollowPitch = false;
    }

    public class MemorySettings
    {
        public bool enableSmartGC = false;
        public float gcInterval = 60f;
        public bool gcInGame = false;
    }

    public class CompatibilitySettings
    {
        public bool enableLegacyPauseFix = false;
        public bool enableNoFailTooEarly = false;
    }
}
