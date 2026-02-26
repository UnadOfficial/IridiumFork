using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Iridium.Patches
{
    /// <summary>
    /// Judge Text Patches - Customizes judge text display with offset support
    /// </summary>
    public static class JudgeTextPatches
    {
        private static float _lastHitAngleOffset = 0f;
        private static float _lastBpmTimesSpeed = 1f;
        private static bool _hasValidOffset = false;

        /// <summary>
        /// Get display text for a HitMargin
        /// </summary>
        private static string GetDisplayText(int hitMargin)
        {
            var settings = Main.Settings.judgeText;
            
            if (settings.showAsOffset)
            {
                if (_hasValidOffset)
                {
                    double offsetMs = AngleOffsetToMilliseconds(_lastHitAngleOffset, _lastBpmTimesSpeed);
                    
                    // 检查结果是否有效
                    if (double.IsNaN(offsetMs) || double.IsInfinity(offsetMs))
                    {
                        return "0ms";
                    }
                    
                    return $"{(offsetMs >= 0 ? "" : "-")}{Math.Abs(offsetMs):F0}ms";
                }
                return "0ms";
            }
            else
            {
                return settings.GetTextForHitMargin(hitMargin);
            }
        }

        /// <summary>
        /// 将角度偏移转换为毫秒
        /// </summary>
        private static double AngleOffsetToMilliseconds(float angleOffsetRad, float bpmTimesSpeed)
        {
            // 防止除以零或无效值
            if (bpmTimesSpeed <= 0 || float.IsNaN(bpmTimesSpeed) || float.IsInfinity(bpmTimesSpeed))
            {
                return 0;
            }
            
            if (float.IsNaN(angleOffsetRad) || float.IsInfinity(angleOffsetRad))
            {
                return 0;
            }

            // 使用与游戏相同的计算方式
            double timeInSeconds = scrMisc.AngleToTime(angleOffsetRad, bpmTimesSpeed);
            
            return timeInSeconds * 1000.0;
        }

        /// <summary>
        /// Patch for scrMisc.GetHitMargin - Capture angle offset when hit margin is calculated
        /// </summary>
        [HarmonyPatch(typeof(scrMisc), "GetHitMargin")]
        public static class GetHitMarginPatch
        {
            public static void Postfix(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, HitMargin __result)
            {
                if (!Main.Settings.judgeText.enableJudgeTextCustomization) return;
                if (!Main.Settings.judgeText.showAsOffset) return;

                // 计算角度偏移（与 GetHitMargin 中相同的计算方式）
                float angleOffset = (hitangle - refangle) * (isCW ? 1f : -1f);
                
                _lastHitAngleOffset = angleOffset;
                _lastBpmTimesSpeed = bpmTimesSpeed;
                _hasValidOffset = true;
            }
        }

        /// <summary>
        /// Patch for scrHitTextMesh.Init - Override text initialization
        /// </summary>
        [HarmonyPatch(typeof(scrHitTextMesh), "Init")]
        public static class HitTextMeshInitPatch
        {
            public static void Postfix(scrHitTextMesh __instance, HitMargin hitMargin, TextMesh ___text)
            {
                if (!Main.Settings.judgeText.enableJudgeTextCustomization) return;
                
                ___text.text = GetDisplayText((int)hitMargin);
            }
        }

        /// <summary>
        /// Patch for scrHitTextMesh.Show - Ensure text is updated on show
        /// </summary>
        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        public static class HitTextMeshShowPatch
        {
            public static void Prefix(scrHitTextMesh __instance, HitMargin __instance_hitMargin, TextMesh ___text)
            {
                if (!Main.Settings.judgeText.enableJudgeTextCustomization) return;
                
                if (___text != null)
                {
                    ___text.text = GetDisplayText((int)__instance_hitMargin);
                }
            }
        }
    }
}
