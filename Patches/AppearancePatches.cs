using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using HarmonyLib;

namespace Iridium.Patches
{
    public static class AppearancePatches
    {
        private static Texture2D? bgTex;
        private static RenderTexture? videoRT;
        private static VideoPlayer? videoPlayer;
        private static bool hooked = false;
        private static readonly string[] MENU_SCENES = ["scnLevelSelect", "scnCLS", "scnTaroMenu0"];

        private static string currentSceneName = "";
        private static Dictionary<string, int> sceneBackgroundIndices = new();

        [HarmonyPatch(typeof(scnLevelSelect), "Awake")]
        [HarmonyPatch(typeof(scnCLS), "Awake")]
        [HarmonyPatch(typeof(scnTaroMenu0), "Awake")]
        public static class MenuSkinHookPatch
        {
            public static void Postfix()
            {
                currentSceneName = SceneManager.GetActiveScene().name;
                if (!hooked)
                {
                    Camera.onPreCull += OnCameraPreCull;
                    hooked = true;
                }
                UpdateSkin();
            }
        }

        public static void UpdateSkin()
        {
            if (!Main.Settings.appearance.enableMenuSkin || Config.PlaylistManager.ActivePlaylist == null)
            {
                CleanupResources();
                return;
            }

            var playlist = Config.PlaylistManager.ActivePlaylist;
            var sceneConfig = GetConfigForScene(currentSceneName, playlist);
            
            if (sceneConfig == null || sceneConfig.backgroundRefs.Count == 0)
            {
                CleanupResources();
                return;
            }

            string? backgroundId = GetNextBackgroundId(currentSceneName, sceneConfig);
            if (string.IsNullOrEmpty(backgroundId))
            {
                CleanupResources();
                return;
            }

            var item = playlist.items.FirstOrDefault(i => i.id == backgroundId);
            if (item == null || string.IsNullOrEmpty(item.path) || !File.Exists(item.path))
            {
                CleanupResources();
                return;
            }

            if (item.type == Config.BackgroundType.Video)
            {
                LoadVideo(item.path, sceneConfig);
            }
            else
            {
                LoadImage(item.path);
            }
        }

        private static Config.SceneConfig? GetConfigForScene(string sceneName, Config.Playlist playlist)
        {
            var entry = playlist.sceneConfigs.FirstOrDefault(s => s.sceneName == sceneName);
            if (entry != null && entry.config.backgroundRefs.Count > 0) return entry.config;
            var globalEntry = playlist.sceneConfigs.FirstOrDefault(s => s.sceneName == "Global");
            return globalEntry?.config;
        }

        private static string? GetNextBackgroundId(string sceneName, Config.SceneConfig config)
        {
            if (config.backgroundRefs.Count == 0) return null;

            if (!sceneBackgroundIndices.TryGetValue(sceneName, out int index))
            {
                index = -1;
            }

            switch (config.playbackMode)
            {
                case Config.PlaybackMode.Static:
                    index = 0;
                    break;
                case Config.PlaybackMode.Random:
                    index = UnityEngine.Random.Range(0, config.backgroundRefs.Count);
                    break;
                case Config.PlaybackMode.Sequential:
                case Config.PlaybackMode.Loop:
                    index = (index + 1) % config.backgroundRefs.Count;
                    break;
            }

            sceneBackgroundIndices[sceneName] = index;
            return config.backgroundRefs[index];
        }

        private static void LoadImage(string path)
        {
            // Cleanup video resources
            if (videoPlayer != null) { videoPlayer.Stop(); UnityEngine.Object.Destroy(videoPlayer.gameObject); videoPlayer = null; }
            if (videoRT != null) { videoRT.Release(); UnityEngine.Object.Destroy(videoRT); videoRT = null; }

            if (bgTex != null) UnityEngine.Object.Destroy(bgTex);
            
            try
            {
                byte[] data = File.ReadAllBytes(path);
                bgTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(bgTex, data))
                {
                    bgTex.wrapMode = TextureWrapMode.Clamp;
                    bgTex.filterMode = FilterMode.Bilinear;
                }
            }
            catch (Exception e)
            {
                Main.Logger?.Error($"Failed to load background image: {e.Message}");
            }
        }

        private static void LoadVideo(string path, Config.SceneConfig config)
        {
            if (bgTex != null) { UnityEngine.Object.Destroy(bgTex); bgTex = null; }

            if (videoPlayer == null)
            {
                GameObject go = new("Iridium_MenuBG_VideoPlayer");
                UnityEngine.Object.DontDestroyOnLoad(go);
                videoPlayer = go.AddComponent<VideoPlayer>();
            }

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = path;
            videoPlayer.isLooping = config.loopVideo;
            videoPlayer.playbackSpeed = config.playbackSpeed;
            videoPlayer.audioOutputMode = config.audioEnabled ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
            if (config.audioEnabled) videoPlayer.SetDirectAudioVolume(0, config.audioVolume);
            
            if (videoRT != null) videoRT.Release();
            videoRT = new RenderTexture(Screen.width, Screen.height, 24);
            videoPlayer.targetTexture = videoRT;
            videoPlayer.Play();
        }

        [HarmonyPatch(typeof(scrFloor), "Start")]
        public static class FloorStartPatch
        {
            public static void Postfix(scrFloor __instance)
            {
                if (IsMenuScene(SceneManager.GetActiveScene().name))
                {
                    UpdateFloorStyle(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(scrFloor),nameof(scrFloor.Reset))]
        public static class FloorRefreshColorPatch
        {
            public static void Postfix(scrFloor __instance)
            {
                if (IsMenuScene(SceneManager.GetActiveScene().name))
                {
                    UpdateFloorStyle(__instance);
                }
            }
        }

        public static void UpdateFloorStyle(scrFloor floor)
        {
            if (!Main.Settings.appearance.enableTrackCustomization) return;

            Color c = Main.Settings.appearance.trackColor;
            float b = Main.Settings.appearance.trackBrightness;
            float a = Main.Settings.appearance.trackOpacity;
            Color finalColor = new Color(c.r * b, c.g * b, c.b * b, a);

            var spriteRenderer = floor.GetComponent<SpriteRenderer>();
            if (spriteRenderer is not null)
            {
                spriteRenderer.color = finalColor;
            }

            var renderers = floor.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                // 排除辉光，避免闪烁
                if (r.name.Contains("Glow")) continue;

                if (r is SpriteRenderer sr) sr.color = finalColor;
                else r.material.color = finalColor;
            }
        }

        // 保持对编辑器特定轨道的处理，但改用更高效的方式
        [HarmonyPatch(typeof(scnLevelSelect), "Update")]
        [HarmonyPatch(typeof(scnCLS), "Update")]
        [HarmonyPatch(typeof(scnTaroMenu0), "Update")]
        public static class EditorFloorUpdatePatch
        {
            public static void Postfix()
            {
                if (!Main.Settings.appearance.enableTrackCustomization) return;

                Color c = Main.Settings.appearance.trackColor;
                float b = Main.Settings.appearance.trackBrightness;
                float a = Main.Settings.appearance.trackOpacity;
                Color finalColor = new Color(c.r * b, c.g * b, c.b * b, a);

                if (scnLevelSelect.instance != null && scnLevelSelect.instance.editorFloor != null)
                {
                    UpdateFloorRenderers(scnLevelSelect.instance.editorFloor, finalColor);
                }
            }
        }

        private static void UpdateFloorRenderers(GameObject floorGo, Color color)
        {
            var renderers = floorGo.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.name.Contains("Glow")) continue;
                if (r is SpriteRenderer sr) sr.color = color;
                else r.material.color = color;
            }
        }

        private static void OnCameraPreCull(Camera cam)
        {
            if (cam.name != "Main Camera") return;
            if (!Array.Exists(MENU_SCENES, s => s == currentSceneName)) return;

            var playlist = Config.PlaylistManager.ActivePlaylist;
            if (playlist == null) return;
            var config = GetConfigForScene(currentSceneName, playlist);
            if (config == null) return;

            Texture? target = null;
            if (videoPlayer != null && videoPlayer.isPlaying) target = videoRT;
            else if (bgTex != null) target = bgTex;

            if (target != null)
            {
                DrawBackground(target, config);
            }
        }

        private static void DrawBackground(Texture tex, Config.SceneConfig config)
        {
            // Implementation of drawing with adjustments (hue, sat, etc.)
            if (_effectMat == null)
            {
                // Simple shader for adjustments if full custom shader not provided
                // For now, use a material with a shader that supports basic adjustments
                _effectMat = new Material(Shader.Find("Hidden/Internal-Colored"));
            }

            float aspect = (float)tex.width / tex.height;
            float screenAspect = (float)Screen.width / Screen.height;
            
            Rect drawRect;
            if (aspect > screenAspect)
            {
                float width = Screen.height * aspect;
                drawRect = new Rect((Screen.width - width) / 2, 0, width, Screen.height);
            }
            else
            {
                float height = Screen.width / aspect;
                drawRect = new Rect(0, (Screen.height - height) / 2, Screen.width, height);
            }

            // Apply adjustments
            // We use GUI.color for brightness and opacity as a simple fallback
            Color oldColor = GUI.color;
            float b = config.brightness;
            GUI.color = new Color(b, b, b, config.opacity);
            
            // Simple parallax if enabled
            if (Main.Settings.appearance.useParallax)
            {
                Vector2 mousePos = Input.mousePosition;
                float offsetX = (mousePos.x / Screen.width - 0.5f) * Main.Settings.appearance.parallaxStrength * 50;
                float offsetY = (mousePos.y / Screen.height - 0.5f) * Main.Settings.appearance.parallaxStrength * 50;
                drawRect.x += offsetX;
                drawRect.y -= offsetY;
            }

            // If we had a real HSB shader, we would set properties on _effectMat here
            // _effectMat.SetFloat("_Hue", config.hue);
            // _effectMat.SetFloat("_Saturation", config.saturation);
            // _effectMat.SetFloat("_Contrast", config.contrast);
            
            Graphics.DrawTexture(drawRect, tex, _effectMat);
            GUI.color = oldColor;
        }

        private static bool IsMenuScene(string sceneName)
        {
            foreach (string s in MENU_SCENES) if (s == sceneName) return true;
            return false;
        }

        private static void CleanupResources()
        {
            if (bgTex != null) { UnityEngine.Object.Destroy(bgTex); bgTex = null; }
            if (videoPlayer != null) { videoPlayer.Stop(); UnityEngine.Object.Destroy(videoPlayer.gameObject); videoPlayer = null; }
            if (videoRT != null) { videoRT.Release(); UnityEngine.Object.Destroy(videoRT); videoRT = null; }
        }

        private static Material? _effectMat;
    }
}
