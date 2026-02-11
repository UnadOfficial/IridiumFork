using System;
using System.Collections.Generic;
using UnityEngine;
using ByteSheep.Events;
using ADOFAI.ModdingConvenience;

namespace Iridium.Utils
{
    public static class FloorUtils
    {
        public static scrFloor AddFloor(float x, float y, Transform? parent = null)
        {
            scrFloor floor = CreateFloor(parent);
            floor.transform.position = new Vector3(x, y);
            return floor;
        }

        public static scrFloor? AddEventFloor(float x, float y, QuickAction action, bool gem, Transform? parent = null)
        {
            scrFloor floor;
            if (gem)
            {
                var floors = GameObject.Find("Floors");
                if (floors == null) return null;
                
                var grid = floors.transform.Find("Grid");
                if (grid == null) return null;

                var outerRing = grid.Find("outer ring");
                if (outerRing == null) return null;

                var changingRoomGem = outerRing.Find("ChangingRoomGem");
                if (changingRoomGem == null) return null;

                var movingGem = changingRoomGem.Find("MovingGem");
                if (movingGem == null) return null;

                floor = UnityEngine.Object.Instantiate(movingGem.gameObject, parent).GetComponent<scrFloor>();
                floor.transform.position = new Vector3(x, y);
                
                var disableComp = floor.GetComponent<scrDisableIfWorldNotComplete>();
                if (disableComp != null) UnityEngine.Object.Destroy(disableComp);
                
                var movingComp = floor.GetComponent<scrMenuMovingFloor>();
                if (movingComp != null) UnityEngine.Object.Destroy(movingComp);
                
                scrGem gemComp = floor.gameObject.GetComponent<scrGem>();
                if (gemComp != null) gemComp.startPosition = (gemComp.endPosition = null);
                
                floor.gameObject.SetActive(true);
            }
            else
            {
                floor = AddFloor(x, y, parent);
                if (floor == null) return null;
            }

            ffxCallFunction callFunc = floor.gameObject.GetOrAddComponent<ffxCallFunction>();
            if (callFunc.ue == null)
            {
                callFunc.ue = new QuickEvent();
                callFunc.ue.persistentCalls = new QuickPersistentCallGroup();
            }
            callFunc.ue.RemoveAllListeners();
            callFunc.ue.AddListener(action);
            callFunc.enabled = true;
            return floor;
        }

        public static scrFloor CreateFloor(Transform? parent = null)
        {
            return UnityEngine.Object.Instantiate(PrefabLibrary.instance.scnLevelSelectFloorPrefab.gameObject, parent).GetComponent<scrFloor>();
        }

        public static GameObject? GetFloorGameObjectAt(float x, float y)
        {
            Collider2D[] colliders = Physics2D.OverlapPointAll(new Vector2(x, y), 1 << LayerMask.NameToLayer("Floor"));
            return (colliders.Length == 0) ? null : colliders[0].gameObject;
        }
    }
}
