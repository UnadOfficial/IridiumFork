using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using HarmonyLib;
using Iridium.Config;

namespace Iridium.Patches
{
    public static class AppearancePatches
    {
        private static Camera? targetCam;
        private static bool hooked = false;
        private static string lastScene = "";
        
        // Single/PerScene Resources
        private static Texture2D? singleTex;
        private static RenderTexture? singleRT;
        private static VideoPlayer? singlePlayer;
        private static SkinConfig? currentConfig;

        // Slideshow Resources
        private static int slideshowIndex = 0;
        private static float slideshowTimer = 0f;
        private static Texture2D? slideshowTex;
        private static RenderTexture? slideshowRT;
        private static VideoPlayer? slideshowPlayer;

        private static Material? effectMat;
        private static HashSet<scrFloor> menuFloors = new HashSet<scrFloor>();
        private static Dictionary<scrFloor, Renderer[]> floorRendererCache = new Dictionary<scrFloor, Renderer[]>();
        private static Dictionary<UnityEngine.Object, float> originalAlphaCache = new Dictionary<UnityEngine.Object, float>();
        private static readonly string[] MENU_SCENES = { "scnLevelSelect", "scnCLS", "scnTaroMenu0" };

        public static void OnUpdate(float dt)
        {
            if (Main.Settings.appearance.enableMenuSkin)
            {
                Main.Settings.appearance.EnsureSlideshowSize();
                string sceneName = SceneManager.GetActiveScene().name;

                if (sceneName != lastScene)
                {
                    menuFloors.Clear();
                    floorRendererCache.Clear();
                    originalAlphaCache.Clear();
                    lastScene = sceneName;
                }

                if (IsMenuScene(sceneName))
                {
                    // Find the best camera to hook into
                    Camera? main = Camera.main;
                    // If we find a camera named "Background Camera", it's usually better for backgrounds
                    Camera[] allCameras = Camera.allCameras;
                    foreach (var c in allCameras)
                    {
                        if (c.name == "Background Camera" && c.isActiveAndEnabled)
                        {
                            main = c;
                            break;
                        }
                    }

                    if (main != null)
                    {
                        if (hooked && main != targetCam)
                        {
                            Main.Logger?.Log($"AppearancePatches: Target camera changed from {targetCam?.name} to {main.name}, re-hooking...");
                            Disable();
                        }

                        if (!hooked)
                        {
                            Main.Logger?.Log($"AppearancePatches: Hooking to camera {main.name} in scene {sceneName}");
                            targetCam = main;
                            if (Main.Settings.appearance.mode == SkinMode.Slideshow)
                            {
                                InitSlideshow();
                            }
                            else
                            {
                                StartSingleSkin(sceneName);
                            }
                            Camera.onPreCull += OnCameraPreCull;
                            hooked = true;
                        }

                        if (Main.Settings.appearance.mode == SkinMode.Slideshow)
                        {
                            UpdateSlideshow(dt);
                        }
                    }
                }
                else if (hooked)
                {
                    Main.Logger?.Log($"AppearancePatches: Disabled because scene {sceneName} is not a menu scene");
                    Disable();
                }
            }
            else if (hooked)
            {
                Main.Logger?.Log("AppearancePatches: Disabled because enableMenuSkin is false");
                Disable();
            }

            // Always allow track customization updates if enabled
            if (Main.Settings.appearance.enableTrackCustomization)
            {
                ApplyTrackCustomization();
            }
        }

        private static void StartSingleSkin(string scene)
        {
            CleanupSingle();
            currentConfig = GetCurrentSkinConfig(scene);
            if (currentConfig == null)
            {
                Main.Logger?.Log($"AppearancePatches: No config found for scene {scene}");
                return;
            }
            if (string.IsNullOrEmpty(currentConfig.path))
            {
                Main.Logger?.Log($"AppearancePatches: Path is empty for scene {scene}");
                return;
            }
            if (!File.Exists(currentConfig.path))
            {
                Main.Logger?.Log($"AppearancePatches: File not found at {currentConfig.path}");
                return;
            }

            Main.Logger?.Log($"AppearancePatches: Loading skin from {currentConfig.path}");
            LoadSkin(currentConfig.path, ref singleTex, ref singleRT, ref singlePlayer, "Iridium_SingleSkin_Player");
        }

        private static void InitSlideshow()
        {
            CleanupSlideshow();
            slideshowIndex = 0;
            slideshowTimer = 0f;
            
            var cfg = Main.Settings.appearance.slideshowSkins[slideshowIndex];
            if (cfg == null || string.IsNullOrEmpty(cfg.path) || !File.Exists(cfg.path)) return;

            LoadSkin(cfg.path, ref slideshowTex, ref slideshowRT, ref slideshowPlayer, "Iridium_Slideshow_Player");
        }

        private static void UpdateSlideshow(float dt)
        {
            if (Main.Settings.appearance.slideshowCount <= 0) return;

            slideshowTimer += dt;
            if (slideshowTimer < Main.Settings.appearance.slideDuration) return;

            slideshowTimer = 0f;
            slideshowIndex = (slideshowIndex + 1) % Main.Settings.appearance.slideshowCount;

            var cfg = Main.Settings.appearance.slideshowSkins[slideshowIndex];
            if (cfg == null || string.IsNullOrEmpty(cfg.path) || !File.Exists(cfg.path)) return;

            LoadSkin(cfg.path, ref slideshowTex, ref slideshowRT, ref slideshowPlayer, "Iridium_Slideshow_Player");
        }

        private static void LoadSkin(string path, ref Texture2D? tex, ref RenderTexture? rt, ref VideoPlayer? player, string playerName)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLower();
                bool isVideo = ext == ".mp4" || ext == ".mov" || ext == ".webm";
                Main.Logger?.Log($"AppearancePatches: Loading {path} (isVideo: {isVideo})");

                if (isVideo)
                {
                    if (tex != null) { UnityEngine.Object.Destroy(tex); tex = null; }
                    if (player == null)
                    {
                        GameObject go = new GameObject(playerName);
                        UnityEngine.Object.DontDestroyOnLoad(go);
                        player = go.AddComponent<VideoPlayer>();
                    }
                    player.source = VideoSource.Url;
                    player.url = path;
                    player.isLooping = true;
                    player.audioOutputMode = VideoAudioOutputMode.None;
                    
                    // Set playback speed if available
                    if (currentConfig != null) player.playbackSpeed = currentConfig.playbackSpeed;
                    else if (Main.Settings.appearance.mode == SkinMode.Slideshow)
                    {
                        var cfg = Main.Settings.appearance.slideshowSkins[slideshowIndex];
                        if (cfg != null) player.playbackSpeed = cfg.playbackSpeed;
                    }
                    
                    if (rt != null) rt.Release();
                    rt = new RenderTexture(Screen.width, Screen.height, 0);
                    player.renderMode = VideoRenderMode.RenderTexture;
                    player.targetTexture = rt;
                    player.errorReceived += (v, msg) => Main.Logger?.Log($"AppearancePatches: Video Error: {msg}");
                    player.Prepare();
                    player.Play();
                    Main.Logger?.Log("AppearancePatches: Video player started");
                }
                else
                {
                    if (player != null) { player.Stop(); UnityEngine.Object.Destroy(player.gameObject); player = null; }
                    if (rt != null) { rt.Release(); UnityEngine.Object.Destroy(rt); rt = null; }
                    
                    if (tex != null) UnityEngine.Object.Destroy(tex);
                    byte[] data = File.ReadAllBytes(path);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(tex, data))
                    {
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Bilinear;
                        Main.Logger?.Log($"AppearancePatches: Image loaded ({tex.width}x{tex.height})");
                    }
                    else
                    {
                        Main.Logger?.Log("AppearancePatches: Failed to load image data");
                    }
                }
            }
            catch (Exception e)
            {
                Main.Logger?.Log($"AppearancePatches: Error loading skin: {e}");
            }
        }

        private static void OnCameraPreCull(Camera cam)
        {
            if (cam != targetCam) return;

            Texture? tex = null;
            SkinConfig? cfg = null;

            if (Main.Settings.appearance.mode == SkinMode.Slideshow)
            { 
                tex = (slideshowRT != null && slideshowPlayer != null && slideshowPlayer.isPlaying) ? (Texture?)slideshowRT : (Texture?)slideshowTex;
                if (slideshowIndex < Main.Settings.appearance.slideshowSkins.Length)
                    cfg = Main.Settings.appearance.slideshowSkins[slideshowIndex];
            }
            else
            { 
                tex = (singleRT != null && singlePlayer != null && singlePlayer.isPlaying) ? (Texture?)singleRT : (Texture?)singleTex;
                cfg = currentConfig;
            }

            if (tex == null || cfg == null) return;

            if (effectMat == null)
            {
                effectMat = new Material(Shader.Find("Unlit/Transparent"));
            }

            effectMat.mainTexture = tex;
            Color finalTint = CalculateFinalTint(cfg);
            
            GL.PushMatrix();
            GL.LoadOrtho();
            effectMat.SetPass(0);
            
            float scale = Mathf.Max(cfg.scale, 0.01f);
            float x = (1f - scale) * 0.5f + cfg.offsetX;
            float y = (1f - scale) * 0.5f - cfg.offsetY;

            GL.Begin(GL.QUADS);
            GL.Color(finalTint);
            GL.TexCoord2(0, 0); GL.Vertex3(x, y, 0);
            GL.TexCoord2(0, 1); GL.Vertex3(x, y + scale, 0);
            GL.TexCoord2(1, 1); GL.Vertex3(x + scale, y + scale, 0);
            GL.TexCoord2(1, 0); GL.Vertex3(x + scale, y, 0);
            GL.End();
            
            GL.PopMatrix();
        }

        private static Color CalculateFinalTint(SkinConfig cfg)
        {
            float b = cfg.brightness;
            float s = cfg.saturation;
            float c = cfg.contrast;
            float h = cfg.hue;
            float opacity = cfg.opacity;
            
            float contrastGain = c;
            float contrastOffset = 0.5f * (1.0f - c);
            
            Color finalTint = new Color(
                (b * contrastGain) + contrastOffset,
                (b * contrastGain) + contrastOffset,
                (b * contrastGain) + contrastOffset,
                opacity
            );

            if (Mathf.Abs(h) > 0.1f)
            {
                float angle = h * Mathf.Deg2Rad;
                float cosA = Mathf.Cos(angle);
                float sinA = Mathf.Sin(angle);
                float r = finalTint.r, g = finalTint.g, b_ = finalTint.b;
                finalTint.r = (.299f + .701f * cosA + .168f * sinA) * r + (.587f - .587f * cosA + .330f * sinA) * g + (.114f - .114f * cosA - .497f * sinA) * b_;
                finalTint.g = (.299f - .299f * cosA - .328f * sinA) * r + (.587f + .413f * cosA + .035f * sinA) * g + (.114f - .114f * cosA + .292f * sinA) * b_;
                finalTint.b = (.299f - .300f * cosA + 1.25f * sinA) * r + (.587f - .588f * cosA - 1.05f * sinA) * g + (.114f + .886f * cosA - .203f * sinA) * b_;
            }

            if (Mathf.Abs(s - 1.0f) > 0.01f)
            {
                float lum = 0.299f * finalTint.r + 0.587f * finalTint.g + 0.114f * finalTint.b;
                finalTint.r = lum + s * (finalTint.r - lum);
                finalTint.g = lum + s * (finalTint.g - lum);
                finalTint.b = lum + s * (finalTint.b - lum);
            }

            return finalTint;
        }

        private static SkinConfig? GetCurrentSkinConfig(string scene)
        {
            switch (Main.Settings.appearance.mode)
            {
                case SkinMode.SingleGlobal: return Main.Settings.appearance.globalSkin;
                case SkinMode.PerScene:
                    if (scene == "scnLevelSelect") return Main.Settings.appearance.mainUISkin;
                    if (scene == "scnCLS") return Main.Settings.appearance.clsSkin;
                    if (scene == "scnTaroMenu0") return Main.Settings.appearance.dlcUISkin;
                    break;
            }
            return null;
        }

        private static bool IsMenuScene(string sceneName)
        {
            foreach (string s in MENU_SCENES) if (s == sceneName) return true;
            return false;
        }

        public static void Disable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            CleanupSingle();
            CleanupSlideshow();
            targetCam = null;
            hooked = false;
        }

        private static void CleanupSingle()
        {
            if (singleTex != null) { UnityEngine.Object.Destroy(singleTex); singleTex = null; }
            if (singlePlayer != null) { singlePlayer.Stop(); UnityEngine.Object.Destroy(singlePlayer.gameObject); singlePlayer = null; }
            if (singleRT != null) { singleRT.Release(); UnityEngine.Object.Destroy(singleRT); singleRT = null; }
            currentConfig = null;
        }

        private static void CleanupSlideshow()
        {
            if (slideshowTex != null) { UnityEngine.Object.Destroy(slideshowTex); slideshowTex = null; }
            if (slideshowPlayer != null) { slideshowPlayer.Stop(); UnityEngine.Object.Destroy(slideshowPlayer.gameObject); slideshowPlayer = null; }
            if (slideshowRT != null) { slideshowRT.Release(); UnityEngine.Object.Destroy(slideshowRT); slideshowRT = null; }
            slideshowTimer = 0f;
        }

        // Keep Track Customization Patches
        [HarmonyPatch(typeof(scrFloor), "Start")]
        public static class FloorStartPatch
        {
            public static void Postfix(scrFloor __instance)
            {
                if (IsMenuScene(SceneManager.GetActiveScene().name)) 
                {
                    menuFloors.Add(__instance);
                    UpdateFloorStyle(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(scrFloor), nameof(scrFloor.Reset))]
        public static class FloorRefreshColorPatch
        {
            public static void Postfix(scrFloor __instance)
            {
                if (IsMenuScene(SceneManager.GetActiveScene().name)) UpdateFloorStyle(__instance);
            }
        }

        public static void UpdateFloorStyle(scrFloor floor)
        {
            if (!Main.Settings.appearance.enableTrackCustomization) return;
            if (floor == null) return;

            Color c = Main.Settings.appearance.trackColor;
            float b = Main.Settings.appearance.trackBrightness;
            float a = Main.Settings.appearance.trackOpacity;
            
            // Helper to get or cache original alpha
            float GetTargetAlpha(UnityEngine.Object obj, float currentAlpha)
            {
                if (!originalAlphaCache.TryGetValue(obj, out float originalAlpha))
                {
                    originalAlpha = currentAlpha;
                    originalAlphaCache[obj] = originalAlpha;
                }
                return Mathf.Min(originalAlpha, a);
            }

            // 1. Main SpriteRenderer
            SpriteRenderer mainSr = floor.GetComponent<SpriteRenderer>();
            if (mainSr != null)
            {
                float targetA = GetTargetAlpha(mainSr, mainSr.color.a);
                mainSr.color = new Color(c.r * b, c.g * b, c.b * b, targetA);
            }

            // 2. Children Renderers
            if (!floorRendererCache.TryGetValue(floor, out var renderers))
            {
                renderers = floor.GetComponentsInChildren<Renderer>(true);
                floorRendererCache[floor] = renderers;
            }

            foreach (var r in renderers)
            {
                if (r == null) continue;
                
                if (r is SpriteRenderer sr)
                {
                    float targetA = GetTargetAlpha(sr, sr.color.a);
                    sr.color = new Color(c.r * b, c.g * b, c.b * b, targetA);
                }
                else
                {
                    try
                    {
                        if (r.material != null)
                        {
                            float targetA = GetTargetAlpha(r.material, r.material.color.a);
                            r.material.color = new Color(c.r * b, c.g * b, c.b * b, targetA);
                        }
                    }
                    catch { /* Some renderers might not support material.color */ }
                }
            }

            // 3. Specific scrFloor fields
            if (floor.legacyFloorSpriteRenderer != null)
            {
                var sr = floor.legacyFloorSpriteRenderer;
                float targetA = GetTargetAlpha(sr, sr.color.a);
                sr.color = new Color(c.r * b, c.g * b, c.b * b, targetA);
            }

            if (floor.floorRenderer != null && floor.floorRenderer is FloorSpriteRenderer fsr && fsr.renderer != null && fsr.renderer is SpriteRenderer fsrSr)
            {
                float targetA = GetTargetAlpha(fsrSr, fsrSr.color.a);
                fsrSr.color = new Color(c.r * b, c.g * b, c.b * b, targetA);
            }
        }

        [HarmonyPatch(typeof(scnLevelSelect), "Update")]
        [HarmonyPatch(typeof(scnCLS), "Update")]
        [HarmonyPatch(typeof(scnTaroMenu0), "Update")]
        public static class EditorFloorUpdatePatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ret)
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AppearancePatches), nameof(ApplyTrackCustomization)));
                    }
                    yield return codes[i];
                }
            }
        }

        public static void ApplyTrackCustomization()
        {
            if (!Main.Settings.appearance.enableTrackCustomization) return;

            // Ported logic: Find all floors in the scene to ensure everything is covered
            scrFloor[] allFloors = UnityEngine.Object.FindObjectsOfType<scrFloor>();
            foreach (var floor in allFloors)
            {
                if (floor != null)
                {
                    UpdateFloorStyle(floor);
                }
            }

            // Specific handling for level select editor floor
            if (scnLevelSelect.instance != null && scnLevelSelect.instance.editorFloor != null)
            {
                Color c = Main.Settings.appearance.trackColor;
                float b = Main.Settings.appearance.trackBrightness;
                float a = Main.Settings.appearance.trackOpacity;
                Color finalColor = new Color(c.r * b, c.g * b, c.b * b, a);
                UpdateFloorRenderers(scnLevelSelect.instance.editorFloor, finalColor);
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
    }
}