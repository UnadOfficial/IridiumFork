using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches
{
    public static class JsonPatches
    {
        private static readonly Dictionary<char, double> PathAngleMap = new()
        {
            { 'p', 15 }, { 'J', 30 }, { 'E', 45 }, { 'T', 60 }, { 't', 60 },
            { 'o', 75 }, { 'U', 90 }, { 'q', 105 }, { 'G', 120 }, { 'h', 120 },
            { 'Q', 135 }, { 'H', 150 }, { 'W', 165 }, { 'L', 180 }, { 'x', 195 },
            { 'N', 210 }, { 'Z', 225 }, { 'F', 240 }, { 'j', 240 }, { 'V', 255 },
            { 'D', 270 }, { 'Y', 285 }, { 'B', 300 }, { 'y', 300 }, { 'C', 315 },
            { 'M', 330 }, { 'A', 345 }, { '!', 999 }
        };

        private static string ConvertPathToAngle(string pathData)
        {
            List<string> results = new();
            double currentAngle = 0;

            foreach (char c in pathData)
            {
                switch (c)
                {
                    case '5':
                        currentAngle += 360.0 / 5.0;
                        break;
                    case '6':
                        currentAngle -= 360.0 / 5.0;
                        break;
                    case '7':
                        currentAngle += 360.0 / 7.0;
                        break;
                    case '8':
                        currentAngle -= 360.0 / 7.0;
                        break;
                    default:
                        if (PathAngleMap.TryGetValue(c, out double mappedAngle))
                        {
                            currentAngle = mappedAngle;
                        }
                        else
                        {
                            currentAngle = 0;
                        }
                        break;
                }
                results.Add(currentAngle.ToString(CultureInfo.InvariantCulture));
            }

            return string.Join(", ", results);
        }

        [HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
        public static class LevelDataDecodePatch
        {
            public static void Prefix(Dictionary<string, object> dict)
            {
                if (Main.Settings.compatibility.forceAngleData && dict is not null && dict.TryGetValue("pathData", out object val) && val is string pathData)
                {
                    dict["angleData"] = ConvertPathToAngle(pathData);
                    dict.Remove("pathData");
                }
            }

            public static void Postfix(LevelData __instance)
            {
                var comp = Main.Settings.compatibility;

                if (comp.legacyFlashMode != LegacyBehaviorMode.Default)
                {
                    __instance.legacyFlash = comp.legacyFlashMode == LegacyBehaviorMode.AlwaysOn;
                }

                if (comp.legacyCamRelativeToMode != LegacyBehaviorMode.Default)
                {
                    __instance.legacyCamRelativeTo = comp.legacyCamRelativeToMode == LegacyBehaviorMode.AlwaysOn;
                }
            }
        }
    }
}