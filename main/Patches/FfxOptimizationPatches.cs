using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using DG.Tweening;

namespace Iridium.Patches
{
    public static class FfxOptimizationPatches
    {
        private static readonly HashSet<scrDecoration> _activeDecorations = new();
        private static readonly object _activeLock = new();

        public static void MarkActive(scrDecoration decoration)
        {
            if (decoration == null) return;
            lock (_activeLock)
            {
                _activeDecorations.Add(decoration);
            }
        }

        public static void UnmarkActive(scrDecoration decoration)
        {
            if (decoration == null) return;
            lock (_activeLock)
            {
                _activeDecorations.Remove(decoration);
            }
        }

        internal static bool ShouldUpdate(scrDecoration dec)
        {
            if (dec == null || !dec.GetVisible()) return false;

            if (dec.eventTweens != null && dec.eventTweens.Count > 0)
            {
                foreach (var tween in dec.eventTweens.Values)
                {
                    if (tween != null && tween.IsActive() && tween.IsPlaying())
                    {
                        return true;
                    }
                }
            }

            if (dec.parallax != null && (dec.parallax.multiplier.x != 1f || dec.parallax.multiplier.y != 1f))
            {
                return true;
            }

            return false;
        }
    }
}