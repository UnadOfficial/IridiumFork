using System;
using HarmonyLib;
using Iridium.Config;
using TMPro;
using DG.Tweening;
using UnityEngine;

namespace Iridium.Patches
{
	public static class JudgeTextPatches
	{
		private static JudgeTextSettings Settings => Main.Settings.judgeText;

		private static double CalculateTimingFromAngle(float angularOffset)
		{
			var controller = scrController.instance;
			var conductor = scrConductor.instance;
			if (controller == null || conductor == null) return 0;

			double bpm = conductor.bpm;
			double speed = controller.d_speed;
			double pitch = conductor.song.pitch;

			double standardTiming = angularOffset * (controller.playerOne.planetarySystem.isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);

			return -standardTiming;
		}

		private static string GetOffsetText(double timing)
		{
			if (double.IsNaN(timing) || double.IsInfinity(timing))
				return "0ms";

			long ms = (long)Math.Round(timing);
			return $"{(ms >= 0 ? "+" : "-")}{Math.Abs(ms)}ms";
		}

		[HarmonyPatch(typeof(scrHitTextMesh), "Init")]
		public static class HitTextMeshInitPatch
		{
			public static void Postfix(scrHitTextMesh __instance, HitMargin hitMargin, TextMeshPro ___text)
			{
				if (!Settings.enableJudgeTextCustomization) return;
				if (Settings.showAsOffset) return;

				___text.text = Settings.GetTextForHitMargin((int)hitMargin);
			}
		}

		[HarmonyPatch(typeof(scrHitTextManager), "ShowHitText")]
		public static class HitTextManagerShowPatch
		{
			public static void Prefix(float missAngle, out float __state)
			{
				__state = missAngle;
			}
		}

		[HarmonyPatch(typeof(scrHitTextMesh), "Show")]
		public static class HitTextMeshShowPatch
		{
			public static void Prefix(scrHitTextMesh __instance, TextMeshPro ___text, float __state)
			{
				if (!Settings.enableJudgeTextCustomization || !Settings.showAsOffset) return;

				if (___text != null)
				{
					double timing = CalculateTimingFromAngle(__state);
					___text.text = GetOffsetText(timing);
				}
			}
		}

		[HarmonyPatch(typeof(scrHitTextMesh), "Show")]
		public static class HitTextMeshShowRotationFixPatch
		{
			public static void Postfix(scrHitTextMesh __instance, float __state)
			{
				if (scrController.coopMode) return;
				if (__instance.hitMargin == HitMargin.Perfect) return;

				__instance.transform.DOLocalRotate(
					new Vector3(0f, 0f, __state * 20f),
					2f,
					RotateMode.LocalAxisAdd
				);
			}
		}
	}
}
