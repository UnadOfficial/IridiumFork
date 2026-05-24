using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ADOFAI;

namespace Iridium.Patches
{
    public static class EditorFloorOptimizationPatches
    {
        #region Static State for Incremental Mode

        private static bool _incrementalMode;
        private static bool _incrementalIsInsert;
        private static int _incrementalSeqID;

        #endregion

        #region Reflection Targets

        private static readonly MethodInfo _updateSelectedFloorMethod =
            AccessTools.Method(typeof(scnEditor), "UpdateSelectedFloor");
        private static readonly FieldInfo _refreshDecSpritesField =
            AccessTools.Field(typeof(scnEditor), "refreshDecSprites");
        private static readonly MethodInfo _drawFloorNumsMethod =
            AccessTools.Method(typeof(scnEditor), "DrawFloorNums");
        private static readonly MethodInfo _drawFloorOffsetLinesMethod =
            AccessTools.Method(typeof(scnEditor), "DrawFloorOffsetLines");
        private static readonly FieldInfo _meshFloorField =
            AccessTools.Field(typeof(scrLevelMaker), "meshFloor");
        private static readonly FieldInfo _spriteFloorField =
            AccessTools.Field(typeof(scrLevelMaker), "spriteFloor");

        #endregion

        #region Helpers

        private static bool AnyFloorsHaveHolds(List<scrFloor> floors)
        {
            for (int i = 0; i < floors.Count; i++)
                if (floors[i].holdLength >= 0) return true;
            return false;
        }

        private static bool AnyEventsHavePositionTrack(List<LevelEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
                if (events[i].eventType == LevelEventType.PositionTrack) return true;
            return false;
        }

        #endregion

        #region Patch: InsertCharFloor - Incremental

        [HarmonyPatch(typeof(scnEditor), "InsertCharFloor")]
        public static class InsertCharFloorOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scnEditor __instance, int sequenceID, char floorType)
            {
                if (!Main.Settings.optimizer.enableEditorFloorOptimization) return true;
                if (!Main.Settings.optimizer.incrementalFloorInsert) return true;

                var lm = scrLevelMaker.instance;
                var floors = lm.listFloors;
                if (floors == null || floors.Count < 2) return true;

                try
                {
                    // 1. Insert path data
                    __instance.levelData.pathData = __instance.levelData.pathData.Insert(sequenceID, floorType.ToString());

                    // 2. Convert char to float angle (use scrLevelMaker's conversion)
                    float floorAngle = scrLevelMaker.GetAngleFromFloorCharDirection(floorType);
                    if (floorAngle == 999f) return true; // midspin, fallback

                    // 3. Insert into angleData too (for consistency)
                    __instance.levelData.angleData.Insert(sequenceID, floorAngle);

                    // 4. Sync floorAngles on levelMaker
                    lm.floorAngles = __instance.levelData.angleData.ToArray();

                    // 5. Set incremental mode and re-run InstantiateFloatFloors with reuse
                    _incrementalMode = true;
                    _incrementalIsInsert = true;
                    _incrementalSeqID = sequenceID;

                    // Temporarily save existing first floor's renderer type
                    bool hadMesh = floors.Count > 0 && floors[0].GetComponent<FloorMeshRenderer>() != null;

                    // Run InstantiateFloatFloors - it will reuse existing floors
                    // and only create one new one at the right position
                    lm.InstantiateFloatFloors();

                    _incrementalMode = false;

                    // 6. Apply events to update event icon positions
                    __instance.ApplyEventsToFloors();

                    // 7. Redraw visuals
                    lm.DrawHolds(unfillHolds: false);
                    lm.DrawMultiPlanet(forcePlaying: false);

                    var drawNums = (Action)_drawFloorNumsMethod.CreateDelegate(typeof(Action), __instance);
                    drawNums?.Invoke();

                    var drawOffsetLines = (Action)_drawFloorOffsetLinesMethod.CreateDelegate(typeof(Action), __instance);
                    drawOffsetLines?.Invoke();

                    return false; // skip original
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[EditorFloorOptimization] InsertCharPrefix failed: {e}");
                    _incrementalMode = false;
                    return true; // fallback to original
                }
            }
        }

        #endregion

        #region Patch: InsertFloatFloor - Incremental

        [HarmonyPatch(typeof(scnEditor), "InsertFloatFloor")]
        public static class InsertFloatFloorOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scnEditor __instance, int sequenceID, float floorAngle)
            {
                if (!Main.Settings.optimizer.enableEditorFloorOptimization) return true;
                if (!Main.Settings.optimizer.incrementalFloorInsert) return true;

                var lm = scrLevelMaker.instance;
                var floors = lm.listFloors;
                if (floors == null || floors.Count < 2) return true;

                try
                {
                    // 1. Insert angle data
                    __instance.levelData.angleData.Insert(sequenceID, floorAngle);

                    // 2. Sync floorAngles on levelMaker
                    lm.floorAngles = __instance.levelData.angleData.ToArray();

                    // 3. Set incremental mode
                    _incrementalMode = true;
                    _incrementalIsInsert = true;
                    _incrementalSeqID = sequenceID;

                    lm.InstantiateFloatFloors();

                    _incrementalMode = false;

                    // 4. Apply events to update event icon positions
                    __instance.ApplyEventsToFloors();

                    // 5. Redraw visuals
                    lm.DrawHolds(unfillHolds: false);
                    lm.DrawMultiPlanet(forcePlaying: false);

                    var drawNums = (Action)_drawFloorNumsMethod.CreateDelegate(typeof(Action), __instance);
                    drawNums?.Invoke();

                    var drawOffsetLines = (Action)_drawFloorOffsetLinesMethod.CreateDelegate(typeof(Action), __instance);
                    drawOffsetLines?.Invoke();

                    return false;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[EditorFloorOptimization] InsertFloatPrefix failed: {e}");
                    _incrementalMode = false;
                    return true;
                }
            }
        }

        #endregion

        #region Patch: InstantiateFloatFloors - Reuse floors in editor

        [HarmonyPatch(typeof(scrLevelMaker), "InstantiateFloatFloors")]
        public static class InstantiateFloatFloorsOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scrLevelMaker __instance)
            {
                if (!_incrementalMode) return true; // normal mode - let original run

                try
                {
                    var floors = __instance.listFloors;
                    int targetCount = __instance.floorAngles.Length + 1;

                    if (_incrementalIsInsert)
                    {
                        int seqID = _incrementalSeqID;

                        // Create ONE new floor
                        GameObject prefab = (GameObject)_meshFloorField.GetValue(__instance);
                        if (prefab == null) prefab = (GameObject)_spriteFloorField.GetValue(__instance);
                        if (prefab == null) return true; // fallback

                        GameObject container = GameObject.Find("Floors");
                        if (container == null) container = new GameObject("Floors");

                        // Calculate position for new floor
                        var prevFloor = floors[Math.Max(0, seqID)];
                        double radius = scrController.instance.tileSize;
                        float ang = __instance.floorAngles[Math.Min(seqID, __instance.floorAngles.Length - 1)];
                        double exitAngle = ang == 999f
                            ? prevFloor.entryangle
                            : (-ang + 90f) * (Math.PI / 180.0);
                        Vector3 offset = scrMisc.getVectorFromAngle(exitAngle, radius);
                        Vector3 insertPos = prevFloor.transform.position + offset;

                        // If we're inserting at position 0, use floor 0's position + offset
                        if (seqID == 0)
                        {
                            insertPos = floors[0].transform.position + offset;
                        }
                        else
                        {
                            insertPos = floors[seqID].transform.position + offset;
                        }

                        GameObject newObj = UnityEngine.Object.Instantiate(prefab, insertPos, Quaternion.identity);
                        newObj.transform.parent = container.transform;
                        var newFloor = newObj.GetComponent<scrFloor>();

                        // Insert into list at correct position
                        floors.Insert(seqID + 1, newFloor);

                        // Remove excess floors if we have too many
                        while (floors.Count > targetCount)
                        {
                            var extra = floors[floors.Count - 1];
                            if (extra != null) UnityEngine.Object.DestroyImmediate(extra.gameObject);
                            floors.RemoveAt(floors.Count - 1);
                        }

                        // Now rebuild geometry for all floors from insertion point
                        // Reuse existing floor 0 setup
                        RebuildPositionsAndAngles(__instance, seqID);
                    }
                    else
                    {
                        // Delete mode - floor was already removed from list by our prefix
                        // Remove excess floors
                        while (floors.Count > targetCount)
                        {
                            var extra = floors[floors.Count - 1];
                            if (extra != null) UnityEngine.Object.DestroyImmediate(extra.gameObject);
                            floors.RemoveAt(floors.Count - 1);
                        }

                        // Add missing floors if needed
                        while (floors.Count < targetCount)
                        {
                            GameObject prefab = (GameObject)_meshFloorField.GetValue(__instance);
                            if (prefab == null) prefab = (GameObject)_spriteFloorField.GetValue(__instance);
                            if (prefab == null) break;

                            GameObject container = GameObject.Find("Floors");
                            if (container == null) container = new GameObject("Floors");

                            var newObj = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
                            newObj.transform.parent = container.transform;
                            floors.Add(newObj.GetComponent<scrFloor>());
                        }

                        RebuildPositionsAndAngles(__instance, Math.Max(0, _incrementalSeqID - 1));
                    }

                    return false; // skip original InstantiateFloatFloors
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[EditorFloorOptimization] InstFloatPrefix failed: {e}");
                    _incrementalMode = false;
                    return true; // fallback
                }
            }
        }

        #endregion

        #region Geometry Rebuild

        private static void RebuildPositionsAndAngles(scrLevelMaker lm, int fromSeqID)
        {
            var floors = lm.listFloors;
            var angles = lm.floorAngles;
            if (floors == null || floors.Count == 0 || angles == null) return;

            // Reset floor 0
            var floor0 = floors[0];
            floor0.entryangle = 4.71238899230957; // MathF.PI * 1.5
            floor0.seqID = 0;
            floor0.transform.position = Vector3.zero;
            floor0.hasLit = true;

            Vector3 cumulativePos = Vector3.zero;
            double prevEntryAngle = floor0.entryangle;

            for (int i = 0; i < floors.Count - 1 && i < angles.Length; i++)
            {
                var floor = floors[i];
                var nextFloor = floors[i + 1];

                bool isCCW = true;
                double radius = scrController.instance.tileSize;
                float ang = angles[i];

                // Calculate exit angle
                double exitAngle;
                if (ang == 999f)
                {
                    exitAngle = prevEntryAngle;
                    floor.midSpin = true;
                }
                else
                {
                    exitAngle = (-ang + 90f) * (Math.PI / 180.0);
                    floor.midSpin = false;
                }

                floor.exitangle = exitAngle;
                floor.seqID = i;
                floor.prevfloor = i > 0 ? floors[i - 1] : null;
                floor.nextfloor = nextFloor;
                floor.speed = 1f;
                floor.isCCW = !isCCW;

                // Calculate position offset
                Vector3 offset = scrMisc.getVectorFromAngle(exitAngle, radius);
                cumulativePos += offset;

                // Set next floor properties
                if (nextFloor != null)
                {
                    nextFloor.entryangle = (exitAngle + Math.PI) % (2.0 * Math.PI);
                    nextFloor.seqID = i + 1;
                    nextFloor.transform.position = cumulativePos;
                    nextFloor.floatDirection = ang;
                    nextFloor.prevfloor = floor;
                }

                prevEntryAngle = nextFloor != null ? nextFloor.entryangle : exitAngle;

                // Update floor angle/rotation
                floor.UpdateAngle();
            }

            // Fix last floor's exit angle
            if (floors.Count > 1)
            {
                var lastFloor = floors[floors.Count - 1];
                lastFloor.exitangle = lastFloor.entryangle + Math.PI;
                lastFloor.nextfloor = null;
            }
        }

        #endregion

        #region Patch: DeleteFloor - Incremental (via Transpiler replacing RemakePath)

        [HarmonyPatch(typeof(scnEditor), "DeleteFloor",
            new Type[] { typeof(int), typeof(bool) })]
        public static class DeleteFloorOptimizationPatch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // Always replace RemakePath() call with our version.
                // Settings check happens at runtime in LightweightDeleteRemakePath.
                var list = instructions.ToList();

                // Find pattern: call instance void scnEditor::RemakePath(bool, bool)
                // and replace with our method that has the same signature (editor, bool, bool)
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].opcode == OpCodes.Call &&
                        list[i].operand is MethodInfo method &&
                        method.Name == "RemakePath" &&
                        method.DeclaringType == typeof(scnEditor))
                    {
                        list[i] = new CodeInstruction(OpCodes.Call,
                            AccessTools.Method(typeof(DeleteFloorOptimizationPatch),
                                nameof(LightweightDeleteRemakePath)));
                    }
                }
                return list;
            }

            // Lightweight replacement for RemakePath(bool, bool) in DeleteFloor
            // Signature matches RemakePath(bool applyEventsToFloors, bool remakeLevel)
            // so the stack is balanced (editor, bool, bool -> void)
            public static void LightweightDeleteRemakePath(scnEditor editor, bool applyEventsToFloors, bool remakeLevel)
            {
                if (!Main.Settings.optimizer.enableEditorFloorOptimization ||
                    !Main.Settings.optimizer.incrementalFloorInsert)
                {
                    editor.RemakePath();
                    return;
                }

                try
                {
                    var lm = scrLevelMaker.instance;
                    var floors = lm.listFloors;
                    if (floors == null || floors.Count < 2)
                    {
                        editor.RemakePath();
                        return;
                    }

                    // Data was already removed by DeleteFloor.
                    // We need to: sync floorAngles, let InstantiateFloatFloors handle the rest.
                    lm.floorAngles = editor.levelData.angleData.ToArray();

                    _incrementalMode = true;
                    _incrementalIsInsert = false;
                    // seqID was already used for offset, approximate for geometry rebuild
                    _incrementalSeqID = floors.Count > 1 ? 0 : 0;

                    lm.InstantiateFloatFloors();
                    _incrementalMode = false;

                    // Redraw visuals
                    lm.DrawHolds(unfillHolds: false);
                    lm.DrawMultiPlanet(forcePlaying: false);

                    var drawNums = (Action)_drawFloorNumsMethod.CreateDelegate(typeof(Action), editor);
                    drawNums?.Invoke();
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[EditorFloorOptimization] LightweightDeleteRemakePath failed: {e}");
                    _incrementalMode = false;
                    editor.RemakePath(); // Fallback
                }
            }
        }

        #endregion

        #region Patch: scnEditor.RemakePath - Skip redundant calls

        [HarmonyPatch(typeof(scnEditor), "RemakePath",
            new Type[] { typeof(bool), typeof(bool) })]
        public static class RemakePathRedundancyPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scnEditor __instance, bool applyEventsToFloors, bool remakeLevel)
            {
                if (!Main.Settings.optimizer.enableEditorFloorOptimization) return true;
                if (!Main.Settings.optimizer.skipRedundantRemakePath) return true;

                // Visual-only refresh (e.g. ToggleFloorNums calls RemakePath(false, false))
                // scnGame.RemakePath(false, false) does nothing meaningful
                // scnEditor.RemakePath then calls DrawFloorOffsetLines + DrawHolds + DrawFloorNums + DrawMultiPlanet
                // We skip the scnGame call and do only the minimum editor-level draws.
                if (!applyEventsToFloors && !remakeLevel)
                {
                    var drawNums = (Action)_drawFloorNumsMethod.CreateDelegate(typeof(Action), __instance);
                    drawNums?.Invoke();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Patch: scnGame.RemakePath - Optimize visual-only calls

        [HarmonyPatch(typeof(scnGame), "RemakePath",
            new Type[] { typeof(bool), typeof(bool) })]
        public static class GameRemakePathOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scnGame __instance, bool applyEventsToFloors, bool remakeLevel)
            {
                if (!Main.Settings.optimizer.enableEditorFloorOptimization) return true;
                var editor = ADOBase.editor;
                if (editor == null) return true;

                if (!remakeLevel && !applyEventsToFloors)
                {
                    // Visual-only: just setup conductor
                    ADOBase.conductor.SetupConductorWithLevelData(__instance.levelData);
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Patch: DrawFloorNums

        [HarmonyPatch(typeof(scnEditor), "DrawFloorNums")]
        public static class DrawFloorNumsOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scnEditor __instance)
            {
                if (!Main.Settings.optimizer.enableEditorFloorOptimization) return true;

                var floors = __instance.floors;
                if (floors == null) return false;

                bool showNums = __instance.showFloorNums && !__instance.playMode;
                for (int i = 0; i < floors.Count; i++)
                {
                    var floor = floors[i];
                    if (floor != null && floor.enabled && floor.editorNumText != null)
                        floor.editorNumText.gameObject.SetActive(showNums && !floor.isFake);
                }
                return false;
            }
        }

        #endregion

        #region Patch: DrawFloorOffsetLines - Skip if no PositionTrack events

        [HarmonyPatch(typeof(scnEditor), "DrawFloorOffsetLines")]
        public static class DrawFloorOffsetLinesOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scnEditor __instance)
            {
                if (!Main.Settings.optimizer.enableEditorFloorOptimization) return true;
                if (!Main.Settings.optimizer.skipRedundantRemakePath) return true;

                if (!AnyEventsHavePositionTrack(__instance.events))
                    return false; // no offset lines to draw

                return true;
            }
        }

        #endregion

        #region Patch: OffsetFloorIDsInEvents

        [HarmonyPatch(typeof(scnEditor), "OffsetFloorIDsInEvents")]
        public static class OffsetFloorIDsOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scnEditor __instance, int startFloorID, int offset)
            {
                if (!Main.Settings.optimizer.enableEditorFloorOptimization) return true;
                if (!Main.Settings.optimizer.optimizeOffsetFloorEvents) return true;
                if (offset == 0) return false;

                var events = __instance.events;
                for (int i = 0; i < events.Count; i++)
                    if (events[i].floor > startFloorID)
                        events[i].floor += offset;

                var decorations = __instance.decorations;
                for (int i = 0; i < decorations.Count; i++)
                    if (decorations[i].floor > startFloorID)
                        decorations[i].floor += offset;

                if (_refreshDecSpritesField != null)
                    _refreshDecSpritesField.SetValue(__instance, true);
                return false;
            }
        }

        #endregion

        #region Utility

        public static void ResetState()
        {
            _incrementalMode = false;
        }

        #endregion
    }
}
