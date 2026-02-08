using System;
using System.Collections.Generic;
using UnityEngine;

namespace Iridium.Config
{
    [Serializable]
    public class BackgroundItem
    {
        public string id = Guid.NewGuid().ToString();
        public string name = "";
        public string path = "";
        public BackgroundType type = BackgroundType.Image;
    }

    public enum BackgroundType
    {
        Image,
        Video
    }

    [Serializable]
    public class SceneConfig
    {
        public List<string> backgroundRefs = new();
        public PlaybackMode playbackMode = PlaybackMode.Loop;
        
        public float opacity = 1f;
        public float brightness = 1f;
        public float saturation = 1f;
        public float contrast = 1f;
        public float hue = 0f;
        
        // Video specific
        public bool loopVideo = true;
        public float playbackSpeed = 1f;
        public bool audioEnabled = false;
        public float audioVolume = 1f;
    }

    public enum PlaybackMode
    {
        Sequential,
        Random,
        Loop,
        Static
    }

    [Serializable]
    public class Playlist
    {
        public string id = Guid.NewGuid().ToString();
        public string name = "New Playlist";
        public List<BackgroundItem> items = new();
        public List<SceneConfigEntry> sceneConfigs = new()
        {
            new SceneConfigEntry { sceneName = "Global", config = new SceneConfig() },
            new SceneConfigEntry { sceneName = "scnLevelSelect", config = new SceneConfig() },
            new SceneConfigEntry { sceneName = "scnCLS", config = new SceneConfig() },
            new SceneConfigEntry { sceneName = "scnTaroMenu0", config = new SceneConfig() }
        };
    }

    [Serializable]
    public class SceneConfigEntry
    {
        public string sceneName = "";
        public SceneConfig config = new();
    }
}
