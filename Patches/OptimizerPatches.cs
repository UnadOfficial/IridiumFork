using System;
using System.Collections.Generic;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Profiling;

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
            public static Texture2D CreateProcessedTexture(Texture2D source, int targetW, int targetH)
            {
                var rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
                rt.filterMode = FilterMode.Bilinear;
                Graphics.Blit(source, rt);
                Texture2D result = new(targetW, targetH, TextureFormat.RGBA32, false);
                RenderTexture.active = rt;
                result.ReadPixels(new(0, 0, targetW, targetH), 0, 0);
                result.Apply(false);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                result.name = source.name;
                return result;
            }

            private static int AlignTo4(int val) => Math.Max(4, (val + 2) & ~3);

            public static void Postfix(ref Texture2D __result)
            {
                if (!Main.Settings.enableOptimizer || __result == null || GCS.internalLevelName != null) return;
                if (__result.width < 8 || __result.height < 8) return;

                long oldSize = 0;
                if (!Main.Settings.dontShowSavedMemory) oldSize = Profiler.GetRuntimeMemorySizeLong(__result);

                try
                {
                    double scaleFactor = Main.Settings.divideBy;
                    int newW = (int)Math.Round(__result.width / scaleFactor);
                    int newH = (int)Math.Round(__result.height / scaleFactor);

                    if (newW >= 4 && newH >= 4)
                    {
                        if (!Main.Settings.dontResizeMultipleOf4)
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

                    if (__result.width != newW || __result.height != newH)
                    {
                        decorRatios[__result.name] = new((float)__result.width / newW, (float)__result.height / newH, 1f);
                        var optimized = CreateProcessedTexture(__result, newW, newH);
                        if (!Main.Settings.dontCompress)
                        {
                            optimized.Compress(true);
                            optimized.Apply(false, true);
                        }
                        UnityEngine.Object.Destroy(__result);
                        __result = optimized;
                    }
                    else
                    {
                        decorRatios[__result.name] = Vector3.one;
                        if (!Main.Settings.dontCompress)
                        {
                            __result.Compress(true);
                            __result.Apply(false, true);
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Log("Decoration optimization failed: " + e.Message);
                }

                if (!Main.Settings.dontShowSavedMemory)
                {
                    long newSize = Profiler.GetRuntimeMemorySizeLong(__result);
                    savedVRAM_MB += (oldSize - newSize) / 1048576f;
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
                if (!Main.Settings.enableOptimizer || GCS.internalLevelName != null) return;
                var sprite = __instance.displayedSprite.sprite;
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
            public static void Postfix(scrVisualDecoration __instance)
            {
                if (!Main.Settings.enableOptimizer || GCS.internalLevelName != null) return;
                var sprite = __instance.spriteRenderer?.sprite;
                if (sprite?.texture == null) return;

                if (TryGetDecorRatio(sprite.texture.name, out Vector3 ratio))
                {
                    if (__instance.spriteRenderer != null) __instance.spriteRenderer.transform.localScale = ratio;

                    if (!Main.Settings.dontResizeCollider)
                    {
                        __instance.boxCollider.size = Vector2.Scale(__instance.boxCollider.size, ratio);
                        var prop = typeof(scrVisualDecoration).GetProperty("spriteUnscaledSize", BindingFlags.Instance | BindingFlags.Public);
                        if (prop != null)
                        {
                            var oldSize = (Vector2)prop.GetValue(__instance);
                            prop.SetValue(__instance, Vector2.Scale(oldSize, ratio));
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
                if (!Main.Settings.enableOptimizer || GCS.internalLevelName != null || Main.Settings.dontResizeCollider) return;

                var targets = new HashSet<LevelEvent>(ADOBase.editor.selectedDecorations);
                if (__instance.hoveredDecoration != null)
                {
                    if (ADOBase.editor.decorations.Contains(__instance.hoveredDecoration))
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
                if (!Main.Settings.enableOptimizer || GCS.internalLevelName != null || Main.Settings.dontResizeCollider) return;

                foreach (var ev in ADOBase.editor.selectedDecorations)
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
                if (!Main.Settings.enableOptimizer || GCS.internalLevelName != null || Main.Settings.dontResizeCollider) return;
                var tex = __instance.spriteRenderer?.sprite?.texture;
                if (tex != null && TryGetDecorRatio(tex.name, out Vector3 ratio))
                {
                    __result = Vector2.Scale(__result, ratio);
                }
            }
        }

        [HarmonyPatch(typeof(Light), "shadows", MethodType.Getter)]
        public static class ShadowOptimizationPatch
        {
            public static void Postfix(ref LightShadows __result)
            {
                if (Main.Settings.enableOptimizer && Main.Settings.disableShadows) __result = LightShadows.None;
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "Awake")]
        public static class DecorationUpdateOptimizationPatch
        {
            public static void Postfix(scrVisualDecoration __instance)
            {
                if (!Main.Settings.enableOptimizer || !Main.Settings.optimizeDecorationUpdate) return;
                var eventsField = typeof(scrVisualDecoration).GetField("events", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (eventsField?.GetValue(__instance) is System.Collections.IList { Count: 0 } or null)
                {
                    __instance.enabled = false;
                }
                else if (eventsField == null && __instance.transform.childCount == 0)
                {
                    __instance.enabled = false;
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "UpdateHitbox")]
        public static class DamageBoxScalingPatch
        {
            public static void Postfix(scrVisualDecoration __instance)
            {
                if (!Main.Settings.enableOptimizer || GCS.internalLevelName != null || !__instance.useHitbox || __instance.spriteRenderer == null) return;
                Vector3 ratio = __instance.spriteRenderer.transform.localScale;
                if (__instance.hitboxType == Hitbox.Box)
                {
                    __instance.damageBox.size = Vector2.Scale(__instance.damageBox.size, ratio);
                }
                else if (__instance.hitboxType == Hitbox.Capsule)
                {
                    __instance.damageCapsule.size = Vector2.Scale(__instance.damageCapsule.size, ratio);
                }
                else
                {
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
                if (!Main.Settings.enableOptimizer || GCS.internalLevelName != null || isFinished || !reloadDecorations || Main.Settings.dontShowSavedMemory) return;

                if (savedVRAM_MB > 0.1f)
                {
                    var notify = Notification.instance;
                    notify.ShowEntitlementMessage(true, "optimize.savedMem");
                    var setup = typeof(Notification).GetMethod("SetupNotification", BindingFlags.Instance | BindingFlags.NonPublic);
                    setup?.Invoke(notify, [2f, 0.7f, true]);
                    Main.Logger?.Log(Localization.Get("SavedMemoryLog", savedVRAM_MB.ToString("F2")));
                }
                isFinished = true;
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
