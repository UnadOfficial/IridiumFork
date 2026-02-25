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
        // 存储最近一次判定的角度偏移（弧度）
        private static float _lastHitAngleOffset = 0f;
        private static bool _hasValidOffset = false;

        /// <summary>
        /// Get display text for a HitMargin
        /// </summary>
        private static string GetDisplayText(int hitMargin)
        {
            var settings = Main.Settings.judgeText;
            
            if (settings.showAsOffset)
            {
                // 显示为偏移格式
                if (_hasValidOffset)
                {
                    // 将角度转换为时间（毫秒）
                    double offsetMs = AngleOffsetToMilliseconds(_lastHitAngleOffset);
                    return $"{(offsetMs >= 0 ? "+" : "")}{offsetMs:F0}ms";
                }
                return "0ms";
            }
            else
            {
                // 使用自定义文本
                return settings.GetTextForHitMargin(hitMargin);
            }
        }

        /// <summary>
        /// 将角度偏移转换为毫秒
        /// </summary>
        private static double AngleOffsetToMilliseconds(float angleOffsetRad)
        {
            try
            {
                var conductor = scrConductor.instance;
                if (conductor == null) return 0;

                // 获取 BPM 和速度
                double bpm = conductor.bpm;
                double speed = GCS.currentSpeedTrial;
                double bpmTimesSpeed = bpm * speed;

                // 使用 AngleToTime 将角度转换为时间（秒）
                // AngleToTime: angle / PI * (60 / bpm)
                // 但这里我们需要考虑 speed 和 pitch
                double timeInSeconds = (angleOffsetRad / Math.PI) * (60.0 / bpmTimesSpeed);
                
                // 转换为毫秒
                return timeInSeconds * 1000.0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Patch for scrMisc.GetHitMargin - Capture angle offset when hit margin is calculated
        /// </summary>
        [HarmonyPatch(typeof(scrMisc), "GetHitMargin")]
        public static class GetHitMarginPatch
        {
            public static void Postfix(float hitangle, float refangle, bool isCW, HitMargin __result)
            {
                if (!Main.Settings.judgeText.enableJudgeTextCustomization) return;
                if (!Main.Settings.judgeText.showAsOffset) return;

                // 计算角度偏移（与 GetHitMargin 中相同的计算方式）
                float angleOffset = (hitangle - refangle) * (isCW ? 1f : -1f);
                
                _lastHitAngleOffset = angleOffset;
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
