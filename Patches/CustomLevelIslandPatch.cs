using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using ADOFAI.ModdingConvenience;
using Iridium.Utils;

namespace Iridium.Patches
{
    [HarmonyPatch(typeof(scnLevelSelect), "Start")]
    public static class CustomLevelIslandPatch
    {
        public static bool inCustomIsland = false;
        public static Vector2 EntrancePos = new Vector2(-1f, 3f);
        
        public static Vector2 IslandTopLeft = new Vector2(7f, -7f);
        
        public static Vector2 ReturnTargetPos = new Vector2(-1f, 4f);

        public static void Postfix()
        {
            if (!Main.Settings.ui.enableCustomLevelIsland) return;

            GameObject outerRing = GameObject.Find("outer ring");
            if (outerRing != null)
            {
                Transform floorCalibration = outerRing.transform.Find("FloorCalibration");
                if (floorCalibration != null)
                {
                    // 1. 克隆传送门外观喵
                    GameObject portalObj = UnityEngine.Object.Instantiate(floorCalibration.gameObject, outerRing.transform);
                    portalObj.name = "IridiumCustomIslandPortal";
                    portalObj.transform.position = new Vector3(EntrancePos.x, EntrancePos.y);
                    
                    // 2. 彻底删除原有的 scrFloor (带跳转逻辑的) 喵
                    scrFloor oldFloor = portalObj.GetComponent<scrFloor>();
                    if (oldFloor != null) UnityEngine.Object.DestroyImmediate(oldFloor);
                    
                    // 3. 重新绑定一个新的、干净的 scrFloor 喵
                    scrFloor newFloor = portalObj.AddComponent<scrFloor>();
                    newFloor.levelnumber = Portal.None;
                    newFloor.isportal = false;
                    newFloor.enabled = true;

                    // 4. 绑定我们的自定义传送逻辑喵
                    ffxCallFunction callFunc = portalObj.GetOrAddComponent<ffxCallFunction>();
                    if (callFunc.ue == null)
                    {                                
                        callFunc.ue = new ByteSheep.Events.QuickEvent();
                        callFunc.ue.persistentCalls = new ByteSheep.Events.QuickPersistentCallGroup();
                    }
                    callFunc.ue.RemoveAllListeners();
                    callFunc.ue.AddListener(() =>
                    {
                        if (!inCustomIsland)
                        {                                    
                            scrCamera.instance.positionState = PositionState.None;
                            scrController.instance.chosenPlanet.MovePlanet(IslandTopLeft, false, null);
                            inCustomIsland = true;
                        }
                    });
                    callFunc.enabled = true;

                    GameObject canvasWorld = GameObject.Find("Canvas World");
                    if (canvasWorld != null)
                {
                        Transform calibrationText = canvasWorld.transform.Find("Calibration");
                        if (calibrationText != null)
                        {
                            Transform textTrans = UnityEngine.Object.Instantiate(calibrationText, canvasWorld.transform);
                            textTrans.position = new Vector3(EntrancePos.x - 0.8f, EntrancePos.y);
                            textTrans.name = "IridiumPortalText";
                            
                            scrTextChanger textChanger = textTrans.GetComponent<scrTextChanger>();
                            if (textChanger != null) textChanger.desktopText = "Iridium Levels";
                            
                            UnityEngine.UI.Text textComp = textTrans.GetComponent<UnityEngine.UI.Text>();
                            if (textComp != null)
                            {
                                textComp.alignment = TextAnchor.MiddleCenter;
                                textComp.text = "Iridium Levels";
                            }
                            textTrans.ScaleXY(0.6f, 0.6f);
                    }
                }
            }

            LoadCustomLevels();
        }
        }

        private static void LoadCustomLevels()
        {
            string levelsPath = Path.Combine(Main.Mod?.Path, "Resources", "levels");
            if (!Directory.Exists(levelsPath))
            {
                Directory.CreateDirectory(levelsPath);
                return;
            }

            string[] directories = Directory.GetDirectories(levelsPath);
            
            int levelCount = directories.Length;
            int rows = Mathf.CeilToInt(levelCount / 3f);
            if (rows < 1) rows = 1;

            // 生成地基
            for (int r = 0; r <= rows; r++) // 多生成一行作为底部或装饰
            {
                for (int c = 0; c < 3; c++)
                {
                    float fx = IslandTopLeft.x + c;
                    float fy = IslandTopLeft.y - r;
                    
                    // 右上角 (c=2, r=0) 改为返回主界面传送门
                    if (c == 2 && r == 0)
                    {
                        FloorUtils.AddEventFloor(fx, fy, () =>
                        {
                            if (inCustomIsland)
                            {
                                scrCamera.instance.positionState = PositionState.CrownIsland;
                                scrController.instance.chosenPlanet.MovePlanet(ReturnTargetPos, false, null);
                                inCustomIsland = false;
                            }
                        }, true);
                        continue;
                    }

                    // 正常的装饰地砖
                    scrFloor baseFloor = FloorUtils.AddFloor(fx, fy);
                    if (baseFloor != null)
                    {
                        baseFloor.floorRenderer.color = ((r + c) % 2 == 0) ? new Color(0f, 0.7f, 1f, 1f) : new Color(0f, 1f, 1f, 1f);
                    }
                }
            }

            // 生成关卡传送门
            for (int i = 0; i < levelCount; i++)
            {
                string dir = directories[i];
                int r = (i / 3) + 1; // 从第二行开始放关卡，因为第一行右上角是返回
                int c = i % 3;
                
                float px = IslandTopLeft.x + c;
                float py = IslandTopLeft.y - r;

                CreateLevelPortal(dir, px, py);
            }
        }

        private static void CreateLevelPortal(string dir, float x, float y)
        {
            string folderName = Path.GetFileName(dir);
            string adofaiPath = "";
            
            // 查找谱面文件
            string[] files = Directory.GetFiles(dir, "*.adofai");
            if (files.Length > 0) adofaiPath = files[0];
            else return;

            // 创建传送门地板
            scrFloor floor = FloorUtils.AddEventFloor(x, y, () =>
            {
                scrController.instance.LoadCustomWorld(adofaiPath, false, null);
            }, false);

            if (floor != null)
            {
                floor.isportal = true;
                floor.floorRenderer.color = new Color(0.5f, 1f, 1f, 1f);
                
                // 确保传送门特效正确生成喵
                GameObject portalEffect = UnityEngine.Object.Instantiate(PrefabLibrary.instance.lastTilePortalPrefab.gameObject, floor.transform);
                portalEffect.transform.localPosition = Vector3.zero;
                
                // 确保特效层级正确喵
                scrPortalParticles particles = portalEffect.GetComponent<scrPortalParticles>();
                if (particles != null)
                {
                    particles.transform.localScale = Vector3.one;
                }

                // 创建文本显示 (不再使用图片)
                string displayText = GetLevelInfoText(dir, folderName);
                
                // 借用原版 Portal 的文字系统但隐藏图片
                scrPortal portal = UnityEngine.Object.Instantiate(RDConstants.data.prefab_worldPortal).GetComponent<scrPortal>();
                portal.transform.position = new Vector2(x - 0.5f, y + 2.5f);
                portal.sprPortal.gameObject.SetActive(false); // 取消图片预览
                portal.statsText.gameObject.SetActive(false);
                portal.sign.worldName.text = displayText; // 支持富文本
                portal.sign.worldName.alignment = TextAnchor.MiddleCenter;
                portal.gameObject.transform.ScaleXY(0.15f);
            }
        }

        private static string GetLevelInfoText(string dir, string folderName)
        {
            string jsonPath = Path.Combine(dir, "level.json");
            string composer = "Unknown";
            string song = folderName;
            string author = "Unknown";

            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    composer = ExtractValue(json, "composer") ?? composer;
                    song = ExtractValue(json, "song") ?? song;
                    author = ExtractValue(json, "author") ?? author;
                }
                catch { }
            }

            // 限制长度并添加省略号
            composer = Truncate(composer, 15);
            song = Truncate(song, 20);
            author = Truncate(author, 15);

            // 格式: Composer - Song \n Map by author (富文本支持)
            return $"<color=#00ffffff>{composer}</color> - <color=#ffffff>{song}</color>\n<size=80%>Map by <color=#ffff00ff>{author}</color></size>";
        }

        private static string? ExtractValue(string json, string key)
        {
            var match = System.Text.RegularExpressions.Regex.Match(json, $"\"{key}\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string Truncate(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLen) return text;
            return text.Substring(0, maxLen - 2) + "..";
        }
    }
}
