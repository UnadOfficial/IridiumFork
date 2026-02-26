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
        private static double _lastTiming = 0;
        private static bool _hasValidTiming = false;

        /// <summary>
        /// Get display text for a HitMargin
        /// </summary>
        private static string GetDisplayText(int hitMargin)
        {
            var settings = Main.Settings.judgeText;
            
            // TODO: showAsOffset 功能暂时禁用
            // if (settings.showAsOffset)
            // {
            //     if (_hasValidTiming)
            //     {
            //         // 检查结果是否有效
            //         if (double.IsNaN(_lastTiming) || double.IsInfinity(_lastTiming))
            //         {
            //             return "0ms";
            //         }
            //         
            //         return $"{(_lastTiming >= 0 ? "" : "-")}{Math.Abs(_lastTiming):F0}ms";
            //     }
            //     return "0ms";
            // }
            // else
            // {
            //     return settings.GetTextForHitMargin(hitMargin);
            // }
            
            return settings.GetTextForHitMargin(hitMargin);
        }

        /// <summary>
        /// Patch for scrPlanet.SwitchChosen - Calculate timing using Overlayer's method
        /// This is called before GetHitMargin, so we capture the timing here
        /// </summary>
        [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
        public static class SwitchChosenPatch
        {
            public static void Prefix(scrPlanet __instance)
            {
                if (!Main.Settings.judgeText.enableJudgeTextCustomization) return;
                if (!Main.Settings.judgeText.showAsOffset) return;

                // 检查是否在游戏中
                if (scrController.instance == null || !scrController.instance.gameworld) 
                {
                    _hasValidTiming = false;
                    return;
                }

                // 使用与 Overlayer 完全一致的计算方式
                // Timing = (angle - targetExitAngle) * direction * 60000 / (Math.PI * bpm * speed * pitch)
                _lastTiming =
                    (__instance.angle - __instance.targetExitAngle)
                    * (scrController.instance.isCW ? 1.0 : -1.0)
                    * 60000.0
                    / (Math.PI * __instance.conductor.bpm * scrController.instance.speed * __instance.conductor.song.pitch);
                
                _hasValidTiming = true;
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