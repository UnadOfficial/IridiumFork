using System;
using System.IO;
using System.Collections.Generic;
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
        private static Camera? targetCam;
        private static readonly string[] MENU_SCENES = ["scnLevelSelect", "scnCLS", "scnTaroMenu0"];

        [HarmonyPatch(typeof(scnLevelSelect), "Awake")]
        public static class MenuSkinHookPatch
        {
            public static void Postfix()
            {
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
            if (!Main.Settings.appearance.enableMenuSkin)
            {
                CleanupResources();
                return;
            }

            string path = Main.Settings.appearance.skinPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                CleanupResources();
                return;
            }

            string ext = Path.GetExtension(path).ToLower();
            string[] videoExts = { ".mp4", ".mov", ".webm" };
            string[] imageExts = { ".png", ".jpg", ".jpeg" };

            if (Array.Exists(videoExts, e => e == ext))
            {
                LoadVideo(path);
            }
            else if (Array.Exists(imageExts, e => e == ext))
            {
                LoadImage(path);
            }
        }

        private static void LoadImage(string path)
        {
            // Cleanup video resources if switching to image
            if (videoPlayer != null) { videoPlayer.Stop(); UnityEngine.Object.Destroy(videoPlayer.gameObject); videoPlayer = null; }
            if (videoRT != null) { videoRT.Release(); UnityEngine.Object.Destroy(videoRT); videoRT = null; }

            if (bgTex != null) UnityEngine.Object.Destroy(bgTex);
            
            byte[] data = File.ReadAllBytes(path);
            bgTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (ImageConversion.LoadImage(bgTex, data))
            {
                bgTex.wrapMode = TextureWrapMode.Clamp;
                bgTex.filterMode = FilterMode.Bilinear;
            }
        }

        private static void LoadVideo(string path)
        {
            // Cleanup image resources if switching to video
            if (bgTex != null) { UnityEngine.Object.Destroy(bgTex); bgTex = null; }

            if (videoPlayer == null)
            {
                GameObject go = new GameObject("Iridium_MenuBG_VideoPlayer");
                UnityEngine.Object.DontDestroyOnLoad(go);
                videoPlayer = go.AddComponent<VideoPlayer>();
            }

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = path;
            videoPlayer.isLooping = Main.Settings.appearance.backgroundLoop;
            videoPlayer.playbackSpeed = Main.Settings.appearance.backgroundPlaybackSpeed;
            videoPlayer.audioOutputMode = Main.Settings.appearance.backgroundAudio ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
            
            if (videoRT != null) videoRT.Release();
            videoRT = new RenderTexture(Screen.width, Screen.height, 0);
            
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRT;
            videoPlayer.Prepare();
            videoPlayer.Play();
        }

        [HarmonyPatch(typeof(scnLevelSelect), "Update")]
        public static class LevelSelectTrackPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LevelSelectTrackPatch), nameof(UpdateTrackStyle)));
                foreach (var instruction in instructions)
                {
                    yield return instruction;
                }
            }

            public static void UpdateTrackStyle()
            {
                if (!Main.Settings.appearance.enableTrackCustomization) return;

                // Find the track in level select. Usually it's under a certain name or component
                // In ADOFAI level select, tracks are often mesh renderers or sprite renderers
                // We'll search for them once per update or use a more efficient way if possible
                var tracks = GameObject.FindObjectsOfType<MeshRenderer>();
                foreach (var renderer in tracks)
                {
                    if (renderer.name.Contains("Track") || renderer.gameObject.layer == LayerMask.NameToLayer("UI"))
                    {
                        // Applying adjustments
                        Color c = Main.Settings.appearance.trackColor;
                        float b = Main.Settings.appearance.trackBrightness;
                        float a = Main.Settings.appearance.trackOpacity;
                        
                        Color finalColor = new Color(c.r * b, c.g * b, c.b * b, a);
                        renderer.material.color = finalColor;
                    }
                }
            }
        }

        private static void OnCameraPreCull(Camera cam)
        {
            if (!Main.Settings.appearance.enableMenuSkin) return;
            if (!IsMenuScene(SceneManager.GetActiveScene().name)) return;

            Texture? targetTex = (videoRT != null && videoPlayer != null && videoPlayer.isPlaying) ? (Texture?)videoRT : (Texture?)bgTex;
            if (targetTex == null) return;

            // Simple parallax effect
            Rect drawRect = new Rect(0, 0, 1, 1);
            if (Main.Settings.appearance.useParallax)
            {
                Vector3 mousePos = Input.mousePosition;
                float offsetX = (mousePos.x / Screen.width - 0.5f) * Main.Settings.appearance.parallaxStrength * 0.1f;
                float offsetY = (mousePos.y / Screen.height - 0.5f) * Main.Settings.appearance.parallaxStrength * 0.1f;
                drawRect = new Rect(-offsetX, -offsetY, 1 + offsetX * 2, 1 + offsetY * 2);
            }

            // --- Advanced Color Adjustments ---
            float b = Main.Settings.appearance.backgroundBrightness;
            float s = Main.Settings.appearance.backgroundSaturation;
            float c = Main.Settings.appearance.backgroundContrast;
            float h = Main.Settings.appearance.backgroundHue; // -180 to 180
            float opacity = Main.Settings.appearance.backgroundOpacity;
            Color tint = Main.Settings.appearance.backgroundColor;

            // Use a material if possible for better effects, but since we are using Graphics.DrawTexture,
            // we can simulate some of it via GUI.color and multiple passes if needed.
            // However, Graphics.DrawTexture with a custom material is the proper way.
            
            if (_effectMat == null)
            {
                // Create a simple hidden shader based material for color adjustments
                // Since we can't easily compile shaders at runtime in Unity without assets,
                // we'll use a built-in one or improve the GUI.color logic.
                // For now, let's use a more robust calculation for GUI.color.
            }

            GL.PushMatrix();
            GL.LoadOrtho();
            
            // Simplified HSV-like tinting via GUI.color
            // 1. Apply Contrast (offset brightness)
            float contrastGain = c;
            float contrastOffset = 0.5f * (1.0f - c);
            
            // 2. Combine with Brightness and Tint
            Color finalTint = new Color(
                (tint.r * b * contrastGain) + contrastOffset,
                (tint.g * b * contrastGain) + contrastOffset,
                (tint.b * b * contrastGain) + contrastOffset,
                opacity
            );

            // 3. Simple Hue Shift approximation (if h != 0)
            if (Mathf.Abs(h) > 0.1f)
            {
                float angle = h * Mathf.Deg2Rad;
                float cosA = Mathf.Cos(angle);
                float sinA = Mathf.Sin(angle);
                
                // Hue rotation matrix approximation
                float r = finalTint.r;
                float g = finalTint.g;
                float b_ = finalTint.b;
                
                finalTint.r = (.299f + .701f * cosA + .168f * sinA) * r + (.587f - .587f * cosA + .330f * sinA) * g + (.114f - .114f * cosA - .497f * sinA) * b_;
                finalTint.g = (.299f - .299f * cosA - .328f * sinA) * r + (.587f + .413f * cosA + .035f * sinA) * g + (.114f - .114f * cosA + .292f * sinA) * b_;
                finalTint.b = (.299f - .300f * cosA + 1.25f * sinA) * r + (.587f - .588f * cosA - 1.05f * sinA) * g + (.114f + .886f * cosA - .203f * sinA) * b_;
            }

            // 4. Saturation approximation
            if (Mathf.Abs(s - 1.0f) > 0.01f)
            {
                float lum = 0.299f * finalTint.r + 0.587f * finalTint.g + 0.114f * finalTint.b;
                finalTint.r = lum + s * (finalTint.r - lum);
                finalTint.g = lum + s * (finalTint.g - lum);
                finalTint.b = lum + s * (finalTint.b - lum);
            }

            Color oldColor = GUI.color;
            GUI.color = finalTint;

            // Flip Y for DrawTexture in Ortho
            Graphics.DrawTexture(new Rect(drawRect.x, drawRect.y + drawRect.height, drawRect.width, -drawRect.height), targetTex);
            
            GUI.color = oldColor;
            GL.PopMatrix();
        }

        private static Material? _effectMat;

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
    }
}
