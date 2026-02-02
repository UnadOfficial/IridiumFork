using System;
using System.Collections.Generic;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Profiling;
using UnityModManagerNet;

namespace Iridium.Patches
{
    public static class OptimizerPatches
    {
        public static Dictionary<string, Vector3> decorRatios = [];
        public static float savedVRAM_MB = 0f;

        public static void ResetDecorOptimization(bool fullReset)
        {
            if (fullReset) decorRatios.Clear();
            savedVRAM_MB = 0f;
            VRAMNotificationPatch.isFinished = false;
        }

        private static bool TryGetDecorRatio(string id, out Vector3 scale)
        {
            if (!decorRatios.ContainsKey(id))
            {
                scale = Vector3.one;
                return false;
            }
            scale = decorRatios[id];
            return true;
        }

        [HarmonyPatch(typeof(TextureManager), "LoadTexture")]
        public static class TextureOptimizationPatch
        {
            public static Texture2D? CreateProcessedTexture(Texture2D source, int targetW, int targetH)
            {
                // Ensure we are on main thread for RenderTexture operations
                if (!Main.IsMainThread)
                {
                    return null;
                }

                RenderTexture? rt = null;
                try
                {
                    rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
                    rt.filterMode = FilterMode.Bilinear;
                    Graphics.Blit(source, rt);
                    
                    Texture2D result = new(targetW, targetH, TextureFormat.RGBA32, false);
                    RenderTexture.active = rt;
                    result.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
                    result.Apply(false);
                    RenderTexture.active = null;
                    result.name = source.name;
                    return result;
                }
                finally
                {
                    if (rt != null)
                    {
                        RenderTexture.ReleaseTemporary(rt);
                    }
                }
            }

            private static int AlignTo4(int val) => Math.Max(4, (val + 2) & ~3);

            public static void Postfix(ref Texture2D? __result)
            {
                if (!Main.Settings.optimizer.enableOptimizer || __result == null || GCS.internalLevelName != null) return;
                
                // Skip small textures (icons, etc)
                if (__result.width <= 32 || __result.height <= 32) return;

                // Safety: if we are not on main thread, we can't use RenderTexture/Blit
                if (!Main.IsMainThread) return;

                long oldSize = 0;
                if (!Main.Settings.optimizer.dontShowSavedMemory) 
                {
                    try { oldSize = Profiler.GetRuntimeMemorySizeLong(__result); } catch { }
                }

                string texName = __result.name;
                try
                {
                    double scaleFactor = Main.Settings.optimizer.divideBy;
                    
                    // If scale is 1.0 and no compression, skip
                    if (scaleFactor <= 1.01 && Main.Settings.optimizer.dontCompress) return;

                    int newW = (int)Math.Round(__result.width / scaleFactor);
                    int newH = (int)Math.Round(__result.height / scaleFactor);

                    if (newW >= 4 && newH >= 4)
                    {
                        if (!Main.Settings.optimizer.dontResizeMultipleOf4)
                        {
                            newW = AlignTo4(newW);
                            newH = AlignTo4(newH);
                        }
                    }
                    else
                    {
                        newW = __result.width;
                        newH = __result.height;
                    }

                    bool resized = false;
                    if (__result.width != newW || __result.height != newH)
                    {
                        var optimized = CreateProcessedTexture(__result, newW, newH);
                        if (optimized != null)
                        {
                            decorRatios[texName] = new((float)__result.width / newW, (float)__result.height / newH, 1f);
                            
                            if (!Main.Settings.optimizer.dontCompress)
                            {
                                // Use highQuality = false for better stability and speed
                                optimized.Compress(false);
                                optimized.Apply(false, true);
                            }
                            else
                            {
                                optimized.Apply(false, false);
                            }

                            // Use DestroyImmediate to free memory RIGHT NOW
                            // This prevents OOM during mass loading
                            UnityEngine.Object.DestroyImmediate(__result);
                            __result = optimized;
                            resized = true;
                        }
                    }
                    
                    if (!resized)
                    {
                        decorRatios[texName] = Vector3.one;
                        if (!Main.Settings.optimizer.dontCompress)
                        {
                            // Already at target size, just compress
                            __result.Compress(false);
                            __result.Apply(false, true);
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Log($"[Optimizer] Optimization failed for {texName}: {e.Message}");
                }

                if (!Main.Settings.optimizer.dontShowSavedMemory)
                {
                    try
                    {
                        long newSize = Profiler.GetRuntimeMemorySizeLong(__result);
                        savedVRAM_MB += (oldSize - newSize) / 1048576f;
                    }
                    catch { }
                }
            }
        }

        [HarmonyPatch(typeof(scnGame))]
        public static class OptimizationResetPatches
        {
            [HarmonyPatch("OnDestroy")]
            [HarmonyPatch("LoadAndPlayLevel")]
            [HarmonyPrefix]
            public static void FullReset() => ResetDecorOptimization(true);

            [HarmonyPatch("LoadLevel"), HarmonyPrefix]
            public static void SoftReset() => ResetDecorOptimization(false);
        }

        [HarmonyPatch(typeof(scrCustomBackgroundSprite), "SetCustomBG")]
        public static class BackgroundScalingPatch
        {
            public static void Postfix(scrCustomBackgroundSprite __instance)
            {
                if (!Main.Settings.optimizer.enableOptimizer || GCS.internalLevelName != null) return;
                var sprite = __instance.displayedSprite?.sprite;
                if (sprite?.texture == null) return;

                if (TryGetDecorRatio(sprite.texture.name, out Vector3 ratio))
                {
                    __instance.imgSize = Vector2.Scale(__instance.imgSize, ratio);
                }
            }
        }

        [HarmonyPatch(typeof(TextureManager), "ApplyOptionsToTexture")]
        public static class TextureNameCleanupPatch
        {
            public static void Postfix(Texture2D texture)
            {
                if (texture.name.EndsWith("(Clone)"))
                {
                    texture.name = texture.name.Substring(0, texture.name.Length - 7);
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "SetSprite", typeof(TextureManager.CustomSprite), typeof(TextureManager.ImageOptions))]
        public static class DecorationScalingPatch
        {
            private static readonly System.Reflection.PropertyInfo? spriteUnscaledSizeProp = typeof(scrVisualDecoration).GetProperty("spriteUnscaledSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            public static void Postfix(scrVisualDecoration __instance)
            {
                if (!Main.Settings.optimizer.enableOptimizer || GCS.internalLevelName != null) return;
                var sprite = __instance.spriteRenderer?.sprite;
                if (sprite?.texture == null) return;

                if (TryGetDecorRatio(sprite.texture.name, out Vector3 ratio))
                {
                    if (__instance.spriteRenderer != null) __instance.spriteRenderer.transform.localScale = ratio;

                    if (!Main.Settings.optimizer.dontResizeCollider)
                    {
                        __instance.boxCollider.size = Vector2.Scale(__instance.boxCollider.size, ratio);
                        if (spriteUnscaledSizeProp != null)
                        {
                            var oldSize = (Vector2)spriteUnscaledSizeProp.GetValue(__instance, null);
                            spriteUnscaledSizeProp.SetValue(__instance, Vector2.Scale(oldSize, ratio), null);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scrDecorationManager), "UpdateBordersSizes")]
        public static class BorderScalingPatch
        {
            public static void Postfix(scrDecorationManager __instance)
            {
                if (!Main.Settings.optimizer.enableOptimizer || GCS.internalLevelName != null || Main.Settings.optimizer.dontResizeCollider) return;

                var selected = ADOBase.editor?.selectedDecorations;
                if (selected == null) return;

                var targets = new HashSet<LevelEvent>(selected);
                if (__instance.hoveredDecoration != null)
                {
                    if (ADOBase.editor != null && ADOBase.editor.decorations.Contains(__instance.hoveredDecoration))
                    {
                        targets.Add(__instance.hoveredDecoration);
                    }
                    else
                    {
                        targets.Remove(__instance.hoveredDecoration);
                    }
                }

                foreach (var ev in targets)
                {
                    if (ev == null) continue;
                    if (scrDecorationManager.GetDecoration(ev) is scrVisualDecoration decor && decor.spriteRenderer?.sprite != null)
                    {
                        float ppu = decor.spriteRenderer.sprite.pixelsPerUnit;
                        float offset = 0.5f / ppu;
                        Vector3 baseScale = decor.transform.localScale;
                        Vector2 sign = new(offset * Mathf.Sign(baseScale.x), offset * Mathf.Sign(baseScale.y));
                        Vector3 ratio = decor.spriteRenderer.transform.localScale;
                        decor.bordersRenderer.size = Vector2.Scale(decor.bordersRenderer.size - sign, ratio) + sign;
                        decor.cachedBorderSize = decor.bordersRenderer.size;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scrDecorationManager), "UpdateHitboxSizes")]
        public static class HitboxScalingPatch
        {
            public static void Postfix()
            {
                if (!Main.Settings.optimizer.enableOptimizer || GCS.internalLevelName != null || Main.Settings.optimizer.dontResizeCollider) return;

                var selected = ADOBase.editor?.selectedDecorations;
                if (selected == null) return;

                foreach (var ev in selected)
                {
                    if (ev != null && scrDecorationManager.GetDecoration(ev) is scrVisualDecoration decor && decor.spriteRenderer != null)
                    {
                        decor.hitboxRenderer.size = Vector2.Scale(decor.hitboxRenderer.size, decor.spriteRenderer.transform.localScale);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "GetDecorationWorldSize")]
        public static class WorldSizeScalingPatch
        {
            public static void Postfix(scrVisualDecoration __instance, ref Vector2 __result)
            {
                if (!Main.Settings.optimizer.enableOptimizer || GCS.internalLevelName != null || Main.Settings.optimizer.dontResizeCollider) return;
                var tex = __instance.spriteRenderer?.sprite?.texture;
                if (tex != null && TryGetDecorRatio(tex.name, out Vector3 ratio))
                {
                    __result = Vector2.Scale(__result, ratio);
                }
            }
        }

        [HarmonyPatch(typeof(scnGame), "ApplyCoreEventsToFloors", typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>), typeof(List<LevelEvent>[]))]
        public static class ShadowOptimizationPatch
        {
            public static void Postfix()
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.disableShadows)
                {
                    QualitySettings.shadows = ShadowQuality.Disable;
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "Awake")]
        public static class DecorationUpdateOptimizationPatch
        {
            public static void Postfix(scrVisualDecoration __instance)
            {
                if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.optimizeDecorationUpdate) return;
                
                // 仅优化渲染器设置，不禁用组件
                // 禁用组件（enabled = false）会导致初始化失败或游戏逻辑中断（导致崩溃）
                var sr = __instance.spriteRenderer;
                if (sr != null)
                {
                    sr.receiveShadows = false;
                    sr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    sr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                }
            }
        }

        [HarmonyPatch(typeof(scrFloor), "Awake")]
        public static class TileUpdateOptimizationPatch
        {
            public static void Postfix(scrFloor __instance)
            {
                if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.optimizeTileUpdate) return;

                // 仅优化渲染器设置，不禁用组件
                // 格子的 enabled 状态对游戏核心逻辑（判定、路径）至关重要，绝不能禁用
                var mr = __instance.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.receiveShadows = false;
                    mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "UpdateHitbox")]
        public static class VisualDecorationUpdateHitboxPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scrVisualDecoration __instance)
            {
                // 如果开启了快速加载且不在编辑器模式，直接跳过碰撞体更新逻辑
                // 这样可以避免大量的 AddComponent/GetComponent 相关的物理计算
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.fastLoading && scnEditor.instance == null)
                {
                    if (__instance.boxCollider != null && __instance.boxCollider.enabled)
                        __instance.boxCollider.enabled = false;
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(scrVisualDecoration __instance)
            {
                if (!Main.Settings.optimizer.enableOptimizer || GCS.internalLevelName != null || !__instance.useHitbox || __instance.spriteRenderer == null) return;
                Vector3 ratio = __instance.spriteRenderer.transform.localScale;
                if (__instance.hitboxType == Hitbox.Box)
                {
                    if (__instance.damageBox != null)
                        __instance.damageBox.size = Vector2.Scale(__instance.damageBox.size, ratio);
                }
                else if (__instance.hitboxType == Hitbox.Capsule)
                {
                    if (__instance.damageCapsule != null)
                        __instance.damageCapsule.size = Vector2.Scale(__instance.damageCapsule.size, ratio);
                }
                else
                {
                    if (__instance.damageCircle != null)
                        __instance.damageCircle.radius *= ratio.x;
                }
            }
        }

        [HarmonyPatch(typeof(scnGame), "UpdateDecorationObjects")]
        public static class VRAMNotificationPatch
        {
            public static bool isFinished = false;
            public static void Postfix(bool reloadDecorations)
            {
                if (!Main.Settings.optimizer.enableOptimizer || GCS.internalLevelName != null || isFinished || !reloadDecorations || Main.Settings.optimizer.dontShowSavedMemory) return;

                if (savedVRAM_MB > 0.1f)
                {
                    VRAMNotificationUI.Show(Localization.Get("SavedMemoryMsg", savedVRAM_MB.ToString("F2")));
                    Main.Logger?.Log(Localization.Get("SavedMemoryLog", savedVRAM_MB.ToString("F2")));
                }
                isFinished = true;
            }
        }

        public class VRAMNotificationUI : MonoBehaviour
        {
            private static VRAMNotificationUI? _instance;
            private string _message = "";
            private float _timer = 0f;
            private const float FadeDuration = 0.5f;
            private const float DisplayDuration = 2.5f;
            private GUIStyle? _style;
            private Texture2D? _background;

            public static void Show(string message)
            {
                if (_instance == null)
                {
                    var go = new GameObject("Iridium_VRAMNotification");
                    _instance = go.AddComponent<VRAMNotificationUI>();
                    DontDestroyOnLoad(go);
                }
                _instance._message = message;
                _instance._timer = FadeDuration + DisplayDuration + FadeDuration;
            }

            private void OnGUI()
            {
                if (_timer <= 0f) return;

                if (_style == null || _background == null)
                {
                    InitializeStyle();
                }

                float alpha = 1f;
                if (_timer > DisplayDuration + FadeDuration)
                {
                    alpha = (FadeDuration + DisplayDuration + FadeDuration - _timer) / FadeDuration;
                }
                else if (_timer < FadeDuration)
                {
                    alpha = _timer / FadeDuration;
                }

                GUI.color = new Color(1f, 1f, 1f, alpha);
                
                // Position: Top-left, Size: 0.75 of a typical toast
                // M3 style: Rounded corners, Surface Container colors
                float width = 240f * 0.75f;
                float height = 50f * 0.75f;
                Rect rect = new(20, 20, width, height);
                
                GUI.Box(rect, "✨ " + _message, _style);
                GUI.color = Color.white;
            }

            private void Update()
            {
                if (_timer > 0f)
                {
                    _timer -= Time.deltaTime;
                }
            }

            private void InitializeStyle()
            {
                Color infoContainer = new(0.1f, 0.2f, 0.35f);      // Info/Secondary Container
                Color onInfoContainer = new(0.7f, 0.85f, 1.0f);    // On Info Container
                
                _background = MakeRoundedTex(128, 128, 16, infoContainer);
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(14 * 0.75f),
                    normal = { background = _background, textColor = onInfoContainer },
                    padding = new RectOffset(8, 8, 4, 4)
                };
            }

            private Texture2D MakeRoundedTex(int width, int height, int radius, Color col)
            {
                Color[] pix = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float dx = x < radius ? radius - x : (x >= width - radius ? x - (width - radius - 1) : 0);
                        float dy = y < radius ? radius - y : (y >= height - radius ? y - (height - radius - 1) : 0);
                        pix[y * width + x] = (dx * dx + dy * dy <= radius * radius) ? col : Color.clear;
                    }
                }
                Texture2D result = new(width, height);
                result.SetPixels(pix);
                result.Apply();
                return result;
            }
        }

        [HarmonyPatch(typeof(RDString), "Get")]
        public static class VRAMTranslationPatch
        {
            public static bool Prefix(string key, ref string __result)
            {
                if (key == "optimize.savedMem")
                {
                    __result = Localization.Get("SavedMemoryMsg", savedVRAM_MB.ToString("F2"));
                    return false;
                }
                return true;
            }
        }
    }
}
