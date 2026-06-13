using System;
using System.Collections.Generic;
using ADOFAI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using HarmonyLib;
using UnityEngine;

namespace Iridium.Patches
{
	public static class ExtremeOptimizationPatches
	{
		#region Configuration

		private const int BATCH_THRESHOLD = 50;
		private const float FRAME_SPREAD = 0.016f;

		#endregion

		#region Tween Batch Processing

		private static class TweenBatchQueue
		{
			private static readonly System.Collections.Generic.Queue<TweenRequest> _pendingTweens = new System.Collections.Generic.Queue<TweenRequest>();
			private static int _tweensCreatedThisFrame = 0;
			private static int _lastDeferredLogFrame = -60;

			private static int MaxTweensPerFrame =>
				Mathf.Clamp(Main.Settings?.optimizer.maxTweensPerFrame ?? 100, 50, 500);

			public static void Enqueue(TweenRequest request)
			{
				_pendingTweens.Enqueue(request);
			}

			public static void StartProcessing()
			{
				ProcessFrame();
			}

			public static void ProcessFrame()
			{
				_tweensCreatedThisFrame = 0;
				int limit = MaxTweensPerFrame;

				while (_pendingTweens.Count > 0 && _tweensCreatedThisFrame < limit)
				{
					var request = _pendingTweens.Dequeue();
					request.Execute();
					_tweensCreatedThisFrame++;
				}

				if (_pendingTweens.Count > 0 && Time.frameCount - _lastDeferredLogFrame >= 60)
				{
					_lastDeferredLogFrame = Time.frameCount;
					Main.Logger?.Log($"[ExtremeOpt] Deferred {_pendingTweens.Count} tween(s)");
				}
			}

			public static void Clear()
			{
				_pendingTweens.Clear();
				_tweensCreatedThisFrame = 0;
			}

			public static int PendingCount => _pendingTweens.Count;
		}

		private abstract class TweenRequest
		{
			public abstract void Execute();
		}

		private sealed class ActionTweenRequest : TweenRequest
		{
			private readonly Action _action;

			public ActionTweenRequest(Action action)
			{
				_action = action;
			}

			public override void Execute() => _action();
		}

		#endregion

		#region MoveTrack Extreme Optimization

		[HarmonyPatch(typeof(ffxMoveFloorPlus), nameof(ffxMoveFloorPlus.StartEffect))]
		public static class ExtremeMoveFloorPatch
		{
			public static bool Prefix(ffxMoveFloorPlus __instance)
			{
				if (!Main.Settings.optimizer.enableOptimizer ||
					!Main.Settings.optimizer.enableExtremeOptimization)
					return true;

				if (Main.Settings.optimizer.optimizeMoveTrack)
					return true;

				try
				{
					__instance.AdjustDurationForHardbake();

					int startIdx = __instance.start;
					int endIdx = __instance.end;
					if (endIdx < startIdx)
					{
						int temp = startIdx;
						startIdx = endIdx;
						endIdx = temp;
					}

					int floorCount = endIdx - startIdx + 1;
					bool isExtremeCase = floorCount > BATCH_THRESHOLD;

					if (!isExtremeCase)
						return true;

					Vector3 posOffset = new Vector3(__instance.targetPos.x, __instance.targetPos.y, 0f);
					float rotZ = __instance.targetRot;
					Vector3 scaleTarget = new Vector3(__instance.targetScaleV2.x, __instance.targetScaleV2.y, 1f);

					var floors = ADOBase.lm.listFloors;
					int count = floors.Count;
					int step = 1 + __instance.gapLength;

					ProcessExtremeMoveFloor(__instance, floors, startIdx, endIdx, step, count,
						posOffset, rotZ, scaleTarget);
					return false;
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[ExtremeOpt] MoveFloor failed: {e}");
					return true;
				}
			}

			private static void ProcessExtremeMoveFloor(ffxMoveFloorPlus instance, List<scrFloor> floors,
				int startIdx, int endIdx, int step, int count,
				Vector3 posOffset, float rotZ, Vector3 scaleTarget)
			{
				int totalFloors = (endIdx - startIdx) / step + 1;
				int floorsPerFrame = Mathf.Max(1, totalFloors / 10);

				int processed = 0;

				for (int i = startIdx; i <= endIdx && i < count; i += step)
				{
					scrFloor target = floors[i];
					Transform targetTransform = target.transform;
					var moveTweens = target.moveTweens;

					Vector3 targetPos = target.startPos + posOffset;
					float finalRotZ = (target.startRot + new Vector3(0, 0, rotZ)).z;

					float delay = (processed / floorsPerFrame) * FRAME_SPREAD;

					if (instance.positionUsed)
					{
						if (!float.IsNaN(targetPos.x))
						{
							if (moveTweens.TryGetValue(TweenType.PositionX, out var oldTx)) oldTx.Kill(true);
							if (!Mathf.Approximately(targetTransform.position.x, targetPos.x))
							{
								var localTransform = targetTransform;
								var localTweens = moveTweens;
								float localTargetX = targetPos.x;
								float localDuration = instance.duration;
								Ease localEase = instance.ease;
								float localDelay = delay;
								TweenBatchQueue.Enqueue(new ActionTweenRequest(() =>
								{
									var tw = DOTween.To(
										() => localTransform.position.x,
										x => localTransform.MoveX(x),
										localTargetX, localDuration)
										.SetEase(localEase)
										.SetDelay(localDelay);
									localTweens[TweenType.PositionX] = tw;
								}));
							}
						}
						if (!float.IsNaN(targetPos.y))
						{
							if (moveTweens.TryGetValue(TweenType.PositionY, out var oldTy)) oldTy.Kill(true);
							if (!Mathf.Approximately(targetTransform.position.y, targetPos.y))
							{
								var localTransform = targetTransform;
								var localTweens = moveTweens;
								float localTargetY = targetPos.y;
								float localDuration = instance.duration;
								Ease localEase = instance.ease;
								float localDelay = delay;
								TweenBatchQueue.Enqueue(new ActionTweenRequest(() =>
								{
									var tw = DOTween.To(
										() => localTransform.position.y,
										y => localTransform.MoveY(y),
										localTargetY, localDuration)
										.SetEase(localEase)
										.SetDelay(localDelay);
									localTweens[TweenType.PositionY] = tw;
								}));
							}
						}
					}

					if (instance.rotationUsed)
					{
						if (moveTweens.TryGetValue(TweenType.Rotation, out var oldTr)) oldTr.Kill(true);
						if (!Mathf.Approximately(targetTransform.eulerAngles.z, finalRotZ))
						{
							var localTarget = target;
							var localTransform = targetTransform;
							var localTweens = moveTweens;
							float localRotZ = finalRotZ;
							float localDuration = instance.duration;
							Ease localEase = instance.ease;
							float localDelay = delay;
							TweenBatchQueue.Enqueue(new ActionTweenRequest(() =>
							{
								var tw = DOTween.To(
									() => localTarget.tweenRot.z,
									r =>
									{
										localTarget.tweenRot.z = r;
										localTransform.eulerAngles = localTarget.tweenRot;
									},
									localRotZ, localDuration)
									.SetEase(localEase)
									.SetDelay(localDelay);
								localTweens[TweenType.Rotation] = tw;
							}));
						}
					}

					if (instance.scaleUsed)
					{
						if (!float.IsNaN(scaleTarget.x))
						{
							if (moveTweens.TryGetValue(TweenType.ScaleX, out var oldSx)) oldSx.Kill(true);
							var localTransform = targetTransform;
							var localTweens = moveTweens;
							float localScaleX = scaleTarget.x;
							float localDuration = instance.duration;
							Ease localEase = instance.ease;
							float localDelay = delay;
							TweenBatchQueue.Enqueue(new ActionTweenRequest(() =>
							{
								var tw = localTransform.DOScaleX(localScaleX, localDuration)
									.SetEase(localEase)
									.SetDelay(localDelay);
								localTweens[TweenType.ScaleX] = tw;
							}));
						}
						if (!float.IsNaN(scaleTarget.y))
						{
							if (moveTweens.TryGetValue(TweenType.ScaleY, out var oldSy)) oldSy.Kill(true);
							var localTransform = targetTransform;
							var localTweens = moveTweens;
							float localScaleY = scaleTarget.y;
							float localDuration = instance.duration;
							Ease localEase = instance.ease;
							float localDelay = delay;
							TweenBatchQueue.Enqueue(new ActionTweenRequest(() =>
							{
								var tw = localTransform.DOScaleY(localScaleY, localDuration)
									.SetEase(localEase)
									.SetDelay(localDelay);
								localTweens[TweenType.ScaleY] = tw;
							}));
						}
					}

					if (instance.opacityUsed)
					{
						if (moveTweens.TryGetValue(TweenType.Opacity, out var oldTo)) oldTo.Kill(true);
						if (!Mathf.Approximately(target.opacity, instance.targetOpacity))
						{
							var localTarget = target;
							var localTweens = moveTweens;
							float localOpacity = instance.targetOpacity;
							float localDuration = instance.duration;
							Ease localEase = instance.ease;
							float localDelay = delay;
							TweenBatchQueue.Enqueue(new ActionTweenRequest(() =>
							{
								var t = localTarget.TweenOpacity(localOpacity, localDuration, localEase);
								if (t != null)
								{
									t.SetDelay(localDelay);
									localTweens[TweenType.Opacity] = t;
								}
							}));
						}
					}

					processed++;
				}

				TweenBatchQueue.StartProcessing();
				Main.Logger?.Log($"[ExtremeOpt] Processed {processed} MoveFloor events in batches");
			}
		}

		#endregion

		#region MoveDecoration Extreme Optimization

		[HarmonyPatch(typeof(ffxMoveDecorationsPlus), nameof(ffxMoveDecorationsPlus.StartEffect))]
		public static class ExtremeMoveDecorPatch
		{
			private static readonly HashSet<scrDecoration> _uniqueDecorations = new();
			private static readonly HashSet<string> _uniqueTags = new(StringComparer.Ordinal);

			public static bool Prefix(ffxMoveDecorationsPlus __instance)
			{
				if (!Main.Settings.optimizer.enableOptimizer ||
					!Main.Settings.optimizer.enableExtremeOptimization)
					return true;

				if (Main.Settings.optimizer.optimizeMoveDecorations)
					return true;

				try
				{
					__instance.AdjustDurationForHardbake();

					int decorCount = 0;
					_uniqueTags.Clear();
					if (__instance.targetTags != null)
					{
						foreach (string tag in __instance.targetTags)
						{
							if (__instance.decManager != null &&
							    __instance.decManager.taggedDecorations != null &&
							    _uniqueTags.Add(tag) &&
							    __instance.decManager.taggedDecorations.TryGetValue(tag, out var taggedList))
							{
								decorCount += taggedList.Count;
							}
						}
					}

					bool isExtremeCase = decorCount > BATCH_THRESHOLD;

					if (!isExtremeCase)
						return true;

					ProcessExtremeMoveDecor(__instance);
					return false;
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[ExtremeOpt] MoveDecor failed: {e}");
					return true;
				}
				finally
				{
					_uniqueTags.Clear();
				}
			}

			private static void ProcessExtremeMoveDecor(ffxMoveDecorationsPlus instance)
			{
				var decorManager = scnGame.suitableDecManager;
				if (decorManager == null) return;

				Vector3 targetPos = instance.targetPos;
				float duration = instance.duration;
				Ease ease = instance.ease;

				try
				{
					_uniqueDecorations.Clear();
					_uniqueTags.Clear();

					foreach (string tag in instance.targetTags)
					{
						if (!_uniqueTags.Add(tag) ||
						    !decorManager.taggedDecorations.TryGetValue(tag, out var taggedList))
						{
							continue;
						}

						foreach (var decor in taggedList)
						{
							if (decor != null)
								_uniqueDecorations.Add(decor);
						}
					}

					int totalDecors = _uniqueDecorations.Count;
					int decorsPerFrame = Mathf.Max(1, totalDecors / 10);

					int processed = 0;

					foreach (var decor in _uniqueDecorations)
					{
						if (decor == null || decor.transform == null) continue;

						float delay = (processed / decorsPerFrame) * FRAME_SPREAD;
						var localTransform = decor.transform;
						Vector3 localTargetPos = targetPos;
						float localDuration = duration;
						Ease localEase = ease;
						float localDelay = delay;
						TweenBatchQueue.Enqueue(new ActionTweenRequest(() =>
						{
							localTransform.DOMove(localTargetPos, localDuration)
								.SetEase(localEase)
								.SetDelay(localDelay);
						}));

						processed++;
					}

					TweenBatchQueue.StartProcessing();
					Main.Logger?.Log($"[ExtremeOpt] Processed {processed} MoveDecor events in batches");
				}
				finally
				{
					_uniqueDecorations.Clear();
					_uniqueTags.Clear();
				}
			}
		}

		#endregion

		#region Update Hook

		[HarmonyPatch(typeof(scnGame), "Update")]
		public static class ProcessPendingTweensPatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				if (!Main.Settings.optimizer.enableOptimizer) return;

				if (TweenBatchQueue.PendingCount > 0)
				{
					TweenBatchQueue.StartProcessing();
				}
			}
		}

		#endregion

		#region Cleanup

		[HarmonyPatch(typeof(scnGame), "OnDestroy")]
		public static class CleanupBatchQueuePatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				TweenBatchQueue.Clear();
				Main.Logger?.Log("[ExtremeOpt] Cleared batch queue");
			}
		}

		#endregion
	}
}
