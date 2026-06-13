using System;
using System.Reflection;
using HarmonyLib;
using Iridium.Config;
using TMPro;
using UnityEngine;

namespace Iridium.Patches
{
    public static class JudgeTextPatches
    {
        private static JudgeTextSettings Settings => Main.Settings.judgeText;

        [HarmonyPatch(typeof(scrController), "Awake_Rewind")]
        public static class ResetTimingOnRewindPatch
        {
            public static void Postfix()
            {
            }
        }
    }
}