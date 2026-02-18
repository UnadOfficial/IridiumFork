using ADOFAI;
using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches
{
    public static class HitPatches
    {
        [HarmonyPatch(typeof(scrHitText), "Init")]
        public static class HitTextPatch
        {
            public static void Postfix(scrHitText __instance, HitMargin hitMargin)
            {
                if (!Main.Settings.hitText.enableHitTextCustomization) return;

                var settings = Main.Settings.hitText;
                var text = __instance.GetComponent<UnityEngine.UI.Text>();
                if (text == null) return;

                // 根据判定类型设置自定义文本和颜色
                switch (hitMargin)
                {
                    case HitMargin.TooEarly:
                        if (!string.IsNullOrEmpty(settings.tooEarlyText))
                            text.text = settings.tooEarlyText;
                        if (settings.tooEarlyColor.HasValue)
                            text.color = settings.tooEarlyColor.Value;
                        break;

                    case HitMargin.VeryEarly:
                        if (!string.IsNullOrEmpty(settings.veryEarlyText))
                            text.text = settings.veryEarlyText;
                        if (settings.veryEarlyColor.HasValue)
                            text.color = settings.veryEarlyColor.Value;
                        break;

                    case HitMargin.EarlyPerfect:
                        if (!string.IsNullOrEmpty(settings.earlyPerfectText))
                            text.text = settings.earlyPerfectText;
                        if (settings.earlyPerfectColor.HasValue)
                            text.color = settings.earlyPerfectColor.Value;
                        break;

                    case HitMargin.Perfect:
                        if (!string.IsNullOrEmpty(settings.perfectText))
                            text.text = settings.perfectText;
                        if (settings.perfectColor.HasValue)
                            text.color = settings.perfectColor.Value;
                        break;

                    case HitMargin.LatePerfect:
                        if (!string.IsNullOrEmpty(settings.latePerfectText))
                            text.text = settings.latePerfectText;
                        if (settings.latePerfectColor.HasValue)
                            text.color = settings.latePerfectColor.Value;
                        break;

                    case HitMargin.VeryLate:
                        if (!string.IsNullOrEmpty(settings.veryLateText))
                            text.text = settings.veryLateText;
                        if (settings.veryLateColor.HasValue)
                            text.color = settings.veryLateColor.Value;
                        break;

                    case HitMargin.TooLate:
                        if (!string.IsNullOrEmpty(settings.tooLateText))
                            text.text = settings.tooLateText;
                        if (settings.tooLateColor.HasValue)
                            text.color = settings.tooLateColor.Value;
                        break;

                    case HitMargin.Multipress:
                        if (!string.IsNullOrEmpty(settings.multipressText))
                            text.text = settings.multipressText;
                        if (settings.multipressColor.HasValue)
                            text.color = settings.multipressColor.Value;
                        break;

                    case HitMargin.OverPress:
                        if (!string.IsNullOrEmpty(settings.overPressText))
                            text.text = settings.overPressText;
                        if (settings.overPressColor.HasValue)
                            text.color = settings.overPressColor.Value;
                        break;
                }
            }
        }
    }
}
