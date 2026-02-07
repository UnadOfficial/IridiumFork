using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches
{
    public static class JsonPatches
    {
        [HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
        public static class LevelDataDecodePatch
        {
            public static void Prefix(Dictionary<string, object> dict)
            {
                if (Main.Settings.compatibility.forceAngleData && dict is not null && dict.TryGetValue("pathData", out object val) && val is string pathData)
                {
                    // RDEditorUtils.DecodeFloatArray 内部使用 foreach (object obj in list)
                    // 所以必须是 List<object>。使用 LINQ 的 Cast<object>().ToList() 是最优雅的写法喵。
                    dict["angleData"] = scrLevelMaker.StringToAngleArray(pathData).Cast<object>().ToList();
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