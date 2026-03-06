using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Iridium.Patches
{
    /// <summary>
    /// Patches for FloorMesh to enable rounded corners for angles 90°-179°.
    /// 
    /// Requirements:
    /// - Angles 0°-90°: Original behavior (rounded corners work)
    /// - Angles 90°-179°: Apply rounded corners
    /// - Angle 180°: NO rounded corner (mesh should be solid)
    /// - Angles 181°-360°: Apply rounded corners via ccw
    /// </summary>
    public static class FloorMeshPatches
    {
        // Private field accessors
        private static readonly FieldInfo f_shortAngle = typeof(FloorMesh)
            .GetField("shortAngle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo f_cornerCenterRadius = typeof(FloorMesh)
            .GetField("cornerCenterRadius", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Calculate num6 (corner radius factor) for extended angle range.
        /// Returns 0 for angles >= 179° (no corner rounding).
        /// </summary>
        public static float CalculateNum6(float shortAngle)
        {
            // shortAngle is always 0°-180° (smallest angle between two directions)
            
            // < 5°: max radius
            if (shortAngle < 0.08726646f)
                return 1f;
            
            // 5° - 30°
            if (shortAngle < 0.5235988f)
            {
                float range = 0.43633235f;
                return Mathf.Lerp(1f, 0.83f, Mathf.Pow((shortAngle - 0.08726646f) / range, 0.5f));
            }
            
            // 30° - 45°
            if (shortAngle < 0.7853982f)
            {
                float range = 0.2617994f;
                return Mathf.Lerp(0.83f, 0.77f, Mathf.Pow((shortAngle - 0.5235988f) / range, 1f));
            }
            
            // 45° - 90°
            if (shortAngle < 1.5707964f)
            {
                float range = 0.7853982f;
                return Mathf.Lerp(0.77f, 0.15f, Mathf.Pow((shortAngle - 0.7853982f) / range, 0.7f));
            }
            
            // 90° - 120° (EXTENDED - original stops here)
            if (shortAngle < 2.0943952f)
            {
                float range = 0.5235988f;
                return Mathf.Lerp(0.15f, 0.1f, Mathf.Pow((shortAngle - 1.5707964f) / range, 0.5f));
            }
            
            // 120° - 150° (EXTENDED)
            if (shortAngle < 2.6179939f)
            {
                float range = 0.5235988f;
                return Mathf.Lerp(0.1f, 0.06f, Mathf.Pow((shortAngle - 2.0943952f) / range, 0.5f));
            }
            
            // 150° - 179° (EXTENDED)
            if (shortAngle < 3.12414f) // ~179°
            {
                float range = 0.5061f;
                return Mathf.Lerp(0.06f, 0.03f, Mathf.Pow((shortAngle - 2.6179939f) / range, 0.5f));
            }
            
            // >= 179° or 180°: NO corner rounding
            return 0f;
        }

        /// <summary>
        /// Postfix patch to override num6 calculation.
        /// </summary>
        [HarmonyPatch(typeof(FloorMesh), "GetPositions")]
        public static class Num6PostfixPatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(FloorMesh __instance, float width)
            {
                float shortAngle = (float)f_shortAngle.GetValue(__instance);
                float num6 = CalculateNum6(shortAngle);
                float newRadius = Mathf.Lerp(0f, width, num6);
                f_cornerCenterRadius.SetValue(__instance, newRadius);
                
                Main.Logger?.Log($"[FloorMeshPatches] shortAngle={shortAngle * 57.29578f:F1}°, num6={num6:F3}, radius={newRadius:F3}");
            }
        }

        /// <summary>
        /// Transpiler patch to modify corner generation thresholds.
        /// - cwCornerPoints: &lt; 120° → &lt; 179° (covers 0°-178.99°)
        /// - ccwCornerPoints: &gt; 250° → &gt; 181° (covers 181°-360°)
        /// - 179°-181° gap: No corner generation (covers 180°)
        /// </summary>
        [HarmonyPatch(typeof(FloorMesh), "GetPositions")]
        public static class CornerThresholdTranspilerPatch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);
                
                const float RAD_CW_ORIG = 2.0942953f;    // ~120°
                const float RAD_CCW_ORIG = 4.3634233f;   // ~250°
                const float RAD_179 = 3.12414f;          // ~179°
                const float RAD_181 = 3.15906f;          // ~181°
                
                int mods = 0;
                
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode != OpCodes.Ldc_R4) continue;
                    
                    float val = (float)code[i].operand;
                    
                    // cwCornerPoints threshold
                    if (Math.Abs(val - RAD_CW_ORIG) < 0.00001f)
                    {
                        code[i] = new CodeInstruction(OpCodes.Ldc_R4, RAD_179);
                        Main.Logger?.Log($"[FloorMeshPatches] cw corner: 120°→179°");
                        mods++;
                    }
                    // ccwCornerPoints threshold
                    else if (Math.Abs(val - RAD_CCW_ORIG) < 0.0001f)
                    {
                        code[i] = new CodeInstruction(OpCodes.Ldc_R4, RAD_181);
                        Main.Logger?.Log($"[FloorMeshPatches] ccw corner: 250°→181°");
                        mods++;
                    }
                }
                
                if (mods == 0)
                {
                    Main.Logger?.Warning("[FloorMeshPatches] Corner threshold: No modifications found!");
                }
                
                return code;
            }
        }
    }
}