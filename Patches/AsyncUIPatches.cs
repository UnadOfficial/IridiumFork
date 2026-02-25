using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ADOFAI;
using ADOFAI.CLS;
using GDMiniJSON;
using HarmonyLib;
using SA.GoogleDoc;
using UnityEngine;

namespace Iridium.Patches
{
    /// <summary>
    /// Async UI Patches for scnCLS - Optimizes level scanning to prevent UI blocking
    /// </summary>
    public static class AsyncUIPatches
    {
        // Scan state tracking
        private static CancellationTokenSource? _scanCancellation;
        private static bool _isScanning = false;

        /// <summary>
        /// Prefix for scnCLS.ScanLevels - Replaces synchronous scanning with async batched processing
        /// </summary>
        [HarmonyPatch(typeof(scnCLS), "ScanLevels")]
        [HarmonyPrefix]
        public static bool ScanLevelsPrefix(scnCLS __instance, CancellationToken cancelToken, bool workshop, bool local, ref Task __result)
        {
            if (!Main.Settings.optimizer.optimizeCLSAsyncScan)
            {
                return true; // Fall back to original if optimization disabled
            }

            __result = ScanLevelsAsync(__instance, cancelToken, workshop, local);
            return false; // Skip original method
        }

        private static async Task ScanLevelsAsync(scnCLS instance, CancellationToken cancelToken, bool workshop, bool local)
        {
            if (_isScanning)
            {
                // Cancel previous scan if still running
                _scanCancellation?.Cancel();
            }

            _isScanning = true;
            _scanCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

            try
            {
                string levelsDir = (string)typeof(scnCLS).GetField("levelsDir", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
                var loadedLevels = (Dictionary<string, GenericDataCLS>)typeof(scnCLS).GetField("loadedLevels", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
                var loadedLevelDirs = (Dictionary<string, string>)typeof(scnCLS).GetField("loadedLevelDirs", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
                var loadedLevelIsDeleted = (Dictionary<string, bool>)typeof(scnCLS).GetField("loadedLevelIsDeleted", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
                var isWorkshopLevel = (Dictionary<string, bool>)typeof(scnCLS).GetField("isWorkshopLevel", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;

                if (local && !Directory.Exists(levelsDir))
                {
                    Debug.LogWarning("First time launching CLS, making directory");
                    RDDirectory.CreateDirectory(levelsDir);
                }
                else
                {
                    bool featuredLevelsMode = (bool)typeof(scnCLS).GetField("featuredLevelsMode", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

                    if (!featuredLevelsMode)
                    {
                        string[] workshopArray = new string[0];
                        string[] localArray = new string[0];
                        Dictionary<string, string[]> workshopTags = new Dictionary<string, string[]>();

                        if (workshop)
                        {
                            // Use reflection to access SteamWorkshop.resultItems
                            var steamWorkshopType = Type.GetType("SteamWorkshop, Assembly-CSharp");
                            var resultItemsProp = steamWorkshopType?.GetProperty("resultItems", BindingFlags.Static | BindingFlags.Public);
                            var resultItems = resultItemsProp?.GetValue(null);
                            
                            if (resultItems is System.Collections.IList list && list.Count > 0)
                            {
                                workshopArray = new string[list.Count];
                                for (int i = 0; i < list.Count; i++)
                                {
                                    var resultItem = list[i];
                                    var pathField = resultItem.GetType().GetField("path");
                                    var tagsField = resultItem.GetType().GetField("tags");
                                    
                                    string path = pathField?.GetValue(resultItem) as string ?? "";
                                    workshopArray[i] = path;
                                    
                                    if (tagsField?.GetValue(resultItem) is string[] tags)
                                    {
                                        workshopTags[path] = tags;
                                    }
                                    
                                    isWorkshopLevel[Path.GetFileName(workshopArray[i])] = true;
                                }
                            }
                        }

                        if (local)
                        {
                            localArray = Directory.GetDirectories(levelsDir);
                        }

                        string[] itemDirs = localArray.Concat(workshopArray).ToArray();
                        cancelToken.ThrowIfCancellationRequested();

                        // Process in batches to avoid blocking main thread
                        const int batchSize = 10;
                        var decodedResults = new List<(int index, string fileName, string path, Dictionary<string, object>? data)>();

                        for (int batch = 0; batch < (itemDirs.Length + batchSize - 1) / batchSize; batch++)
                        {
                            cancelToken.ThrowIfCancellationRequested();
                            
                            int startIdx = batch * batchSize;
                            int endIdx = Math.Min(startIdx + batchSize, itemDirs.Length);

                            var batchTasks = new List<Task<(int index, string fileName, string path, Dictionary<string, object>? data)>>();

                            for (int i = startIdx; i < endIdx; i++)
                            {
                                int idx = i;
                                string dir = itemDirs[i];
                                string levelPath = Path.Combine(dir, "main.adofai");
                                string fileName = Path.GetFileName(dir);
                                bool isDeleted = false;

                                if (loadedLevelIsDeleted.ContainsKey(fileName))
                                {
                                    isDeleted = loadedLevelIsDeleted[fileName];
                                }

                                if (RDFile.Exists(levelPath) && !isDeleted)
                                {
                                    batchTasks.Add(Task.Run(() =>
                                    {
                                        try
                                        {
                                            var data = Json.DeserializePartially(RDFile.ReadAllText(levelPath, null), "actions") as Dictionary<string, object>;
                                            return (idx, fileName, dir, data);
                                        }
                                        catch
                                        {
                                            return (idx, fileName, dir, null);
                                        }
                                    }, cancelToken));
                                }
                                else if (!isDeleted)
                                {
                                    Debug.LogWarning("No level file at " + dir + "!");
                                    batchTasks.Add(Task.FromResult((idx, fileName, dir, (Dictionary<string, object>?)null)));
                                }
                                else
                                {
                                    batchTasks.Add(Task.FromResult((idx, fileName, dir, (Dictionary<string, object>?)null)));
                                }
                            }

                            var batchResults = await Task.WhenAll(batchTasks);
                            decodedResults.AddRange(batchResults);

                            // Yield to main thread between batches
                            await Task.Yield();
                        }

                        cancelToken.ThrowIfCancellationRequested();

                        // Process decoded results on main thread (Unity API safe)
                        foreach (var (index, fileName, path, dictionary) in decodedResults)
                        {
                            if (dictionary != null)
                            {
                                LevelDataCLS levelData = new LevelDataCLS();
                                levelData.Setup();

                                string[] tags;
                                if (workshopTags.TryGetValue(path, out tags))
                                {
                                    levelData.workshopTags = tags;
                                }

                                if (levelData.Decode(dictionary))
                                {
                                    if (!loadedLevels.ContainsKey(fileName))
                                    {
                                        loadedLevels.Add(fileName, levelData);
                                        loadedLevelDirs.Add(fileName, path);
                                        loadedLevelIsDeleted[fileName] = false;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Featured levels mode - use original logic
                        bool techFeaturedLevelsMode = (bool)typeof(scnCLS).GetField("techFeaturedLevelsMode", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                        
                        if (techFeaturedLevelsMode)
                        {
                            // Access techExtraLevels
                            var techExtraLevels = (Dictionary<string, GenericDataCLS>)typeof(scnCLS).GetField("techExtraLevels", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
                            foreach (var kvp in techExtraLevels)
                            {
                                if (!loadedLevels.ContainsKey(kvp.Key))
                                {
                                    loadedLevels.Add(kvp.Key, kvp.Value);
                                    loadedLevelDirs.Add(kvp.Key, string.Empty);
                                    loadedLevelIsDeleted[kvp.Key] = false;
                                    isWorkshopLevel[kvp.Key] = true;
                                }
                            }
                        }
                        else
                        {
                            // Access extraLevels
                            var extraLevels = (Dictionary<string, GenericDataCLS>)typeof(scnCLS).GetField("extraLevels", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
                            foreach (var kvp in extraLevels)
                            {
                                if (!loadedLevels.ContainsKey(kvp.Key))
                                {
                                    loadedLevels.Add(kvp.Key, kvp.Value);
                                    loadedLevelDirs.Add(kvp.Key, string.Empty);
                                    loadedLevelIsDeleted[kvp.Key] = false;
                                    isWorkshopLevel[kvp.Key] = true;
                                }
                            }
                        }
                    }

                    // Update level count
                    typeof(scnCLS).GetField("levelCount", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, loadedLevels.Count);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("CLS scan cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in async CLS scan: {ex}");
            }
            finally
            {
                _isScanning = false;
                _scanCancellation?.Dispose();
                _scanCancellation = null;
            }
        }

        /// <summary>
        /// Optimized CreateFloors - batches tile creation to avoid frame spikes
        /// </summary>
        [HarmonyPatch(typeof(scnCLS), "CreateFloors")]
        [HarmonyPrefix]
        public static bool CreateFloorsPrefix(scnCLS __instance)
        {
            if (!Main.Settings.optimizer.optimizeCLSAsyncScan)
            {
                return true; // Fall back to original if optimization disabled
            }

            // CreateFloors uses many Unity GameObject.Instantiate calls
            // We spread them across frames using a coroutine
            __instance.StartCoroutine(CreateFloorsCoroutine(__instance));
            return false; // Skip original method
        }

        private static System.Collections.IEnumerator CreateFloorsCoroutine(scnCLS instance)
        {
            var loadedLevels = (Dictionary<string, GenericDataCLS>)typeof(scnCLS).GetField("loadedLevels", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var loadedLevelTiles = (Dictionary<string, CustomLevelTile>)typeof(scnCLS).GetField("loadedLevelTiles", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var loadedLevelDirs = (Dictionary<string, string>)typeof(scnCLS).GetField("loadedLevelDirs", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var loadedLevelIsDeleted = (Dictionary<string, bool>)typeof(scnCLS).GetField("loadedLevelIsDeleted", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var sortedLevelKeys = (List<string>)typeof(scnCLS).GetField("sortedLevelKeys", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var newlyInstalledLevelKeys = (List<string>)typeof(scnCLS).GetField("newlyInstalledLevelKeys", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var currentFolderName = (string?)typeof(scnCLS).GetField("currentFolderName", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);
            var floorContainer = (Transform)typeof(scnCLS).GetField("floorContainer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var tilePrefab = (GameObject)typeof(scnCLS).GetField("tilePrefab", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var levelCount = (int)typeof(scnCLS).GetField("levelCount", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);

            if (loadedLevels.Count == 0)
            {
                yield break;
            }

            bool featuredLevelsMode = (bool)typeof(scnCLS).GetField("featuredLevelsMode", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

            if (!featuredLevelsMode)
            {
                string clsLevelsPath = Persistence.DataPath + Path.DirectorySeparatorChar + "clslevels.txt";
                newlyInstalledLevelKeys.Clear();

                if (File.Exists(clsLevelsPath))
                {
                    List<string> existing = new List<string>();
                    string[] saved = File.ReadAllLines(clsLevelsPath);

                    foreach (string key in loadedLevels.Keys)
                    {
                        if (!saved.Contains(key))
                        {
                            newlyInstalledLevelKeys.Add(key);
                        }
                        else
                        {
                            existing.Add(key);
                        }
                    }

                    if (newlyInstalledLevelKeys.Count != 0)
                    {
                        sortedLevelKeys.Clear();
                        sortedLevelKeys.AddRange(newlyInstalledLevelKeys.Union(existing));
                        File.WriteAllLines(clsLevelsPath, sortedLevelKeys);
                    }
                }
                else
                {
                    File.WriteAllLines(clsLevelsPath, sortedLevelKeys);
                }
            }
            else
            {
                // Featured levels new level detection
                newlyInstalledLevelKeys.Clear();
                foreach (string key in loadedLevels.Keys)
                {
                    GenericDataCLS data = loadedLevels[key];
                    bool isNew = false;

                    if (data.isFolder)
                    {
                        foreach (var level in data.folder.containingLevels.Values)
                        {
                            if (Persistence.GetCustomWorldAttempts(level.Hash) == 0)
                            {
                                isNew = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        isNew = Persistence.GetCustomWorldAttempts(data.Hash) == 0;
                    }

                    if (isNew)
                    {
                        newlyInstalledLevelKeys.Add(key);
                    }
                }
            }

            // sortedLevelKeys should be set by optionsPanels.SortedLevelKeys()
            // This happens in the original code after this check
            // We'll skip that part as it's handled elsewhere

            string[] customLevelPaths = GCS.customLevelPaths;
            string directoryName = Path.GetDirectoryName((customLevelPaths != null && customLevelPaths.Length > 0) ? customLevelPaths.Last() : null);
            string fileName = Path.GetFileName(directoryName);
            bool hasLastLevel = directoryName != null && loadedLevels.ContainsKey(fileName);

            CustomLevelTile? lastPlayedTile = null;
            CustomLevelTile? middleTile = null;
            int visibleCount = 0;
            List<CustomLevelTile> visibleTiles = new List<CustomLevelTile>();

            int totalCount = loadedLevels.Count(d => d.Value.parentFolderName != currentFolderName);
            int middleIndex = Mathf.FloorToInt((float)(levelCount - totalCount) / 2f);
            int createdCount = 0;
            const int batchSize = 20; // Create 20 tiles per frame

            foreach (string key in sortedLevelKeys)
            {
                GenericDataCLS data = loadedLevels[key];

                GameObject tileObj = UnityEngine.Object.Instantiate(tilePrefab, floorContainer);
                tileObj.name = key;
                tileObj.GetComponent<scrFloor>().topGlow.gameObject.SetActive(false);

                int yPos = visibleCount - Mathf.FloorToInt((float)(levelCount - totalCount) / 2f);
                tileObj.transform.LocalMoveY(tileObj.transform.localPosition.y - yPos);

                CustomLevelTile tile = tileObj.GetComponent<CustomLevelTile>();
                loadedLevelTiles.Add(key, tile);

                if (data.isLevel)
                {
                    LevelDataCLS level = data.level;
                    if (hasLastLevel && (loadedLevelDirs[key] == directoryName || key == fileName))
                    {
                        lastPlayedTile = tile;
                    }

                    if (level.loadResult == LoadResult.FutureVersion)
                    {
                        tile.MarkUnavailable();
                    }
                }

                if (visibleCount == middleIndex)
                {
                    middleTile = tile;
                }

                tile.levelKey = key;
                string title = RDUtils.RemoveRichTags(data.title);
                bool isNewLevel = newlyInstalledLevelKeys.Contains(key);
                tile.title.text = isNewLevel ? $"<color=#368BE6>{title}</color>" : title;
                tile.artist.text = RDUtils.RemoveRichTags(data.artist);
                tile.image.enabled = false;

                if (data.parentFolderName != currentFolderName)
                {
                    tile.gameObject.SetActive(false);
                }
                else
                {
                    visibleTiles.Add(tile);
                    visibleCount++;
                }

                createdCount++;

                // Yield every batchSize to prevent frame spikes
                if (createdCount % batchSize == 0)
                {
                    yield return null;
                }
            }

            // Update level list objects
            var updateMethod = typeof(scnCLS).GetMethod("UpdateLevelListObjects", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMethod?.Invoke(instance, new object[] { visibleTiles });

            // Select level
            var selectMethod = typeof(scnCLS).GetMethod("SelectLevel", new[] { typeof(CustomLevelTile), typeof(bool) });
            selectMethod?.Invoke(instance, new object[] { lastPlayedTile ?? middleTile!, true });

            // Set planet cosmetic radius
            ADOBase.controller.chosenPlanet.cosmeticRadius = 1f;
        }

        /// <summary>
        /// Optimize SearchLevels - debounce rapid search input
        /// </summary>
        private static Coroutine? _searchCoroutine;
        private static float _searchDebounceTime = 0.15f;
        private static string _pendingSearch = "";
        private static bool _pendingSelect = true;

        [HarmonyPatch(typeof(scnCLS), "SearchLevels")]
        [HarmonyPrefix]
        public static bool SearchLevelsPrefix(scnCLS __instance, string sub, bool alsoSelect)
        {
            if (!Main.Settings.optimizer.optimizeCLSAsyncScan)
            {
                return true; // Fall back to original if optimization disabled
            }

            // Cancel previous search
            if (_searchCoroutine != null)
            {
                __instance.StopCoroutine(_searchCoroutine);
            }

            _pendingSearch = sub;
            _pendingSelect = alsoSelect;

            // Start debounced search
            _searchCoroutine = __instance.StartCoroutine(DebouncedSearch(__instance));
            return false; // Skip original method
        }

        private static System.Collections.IEnumerator DebouncedSearch(scnCLS instance)
        {
            yield return new WaitForSeconds(_searchDebounceTime);

            // Call original SearchLevels with pending parameters
            var searchMethod = typeof(scnCLS).GetMethod("SearchLevels", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // We need to call the original - use reflection to invoke Harmony's original method
            // For simplicity, we'll implement the search logic inline

            var initializing = (bool)typeof(scnCLS).GetField("initializing", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var refreshing = (bool)typeof(scnCLS).GetField("refreshing", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;

            if (initializing || refreshing)
            {
                yield break;
            }

            typeof(scnCLS).GetField("searchParameter", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, _pendingSearch);

            var sortedLevelKeys = (List<string>)typeof(scnCLS).GetField("sortedLevelKeys", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var loadedLevelTiles = (Dictionary<string, CustomLevelTile>)typeof(scnCLS).GetField("loadedLevelTiles", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var loadedLevels = (Dictionary<string, GenericDataCLS>)typeof(scnCLS).GetField("loadedLevels", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
            var currentFolderName = (string?)typeof(scnCLS).GetField("currentFolderName", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);

            List<CustomLevelTile> visibleTiles = new List<CustomLevelTile>();
            string searchLower = _pendingSearch.ToLower();

            foreach (string key in sortedLevelKeys)
            {
                CustomLevelTile tile = loadedLevelTiles[key];
                GenericDataCLS data = loadedLevels[key];
                string[] searchFields = new[] { data.artist, data.author, data.title };

                bool shouldShow = false;

                if (loadedLevels[key].parentFolderName == currentFolderName)
                {
                    if (string.IsNullOrEmpty(_pendingSearch))
                    {
                        shouldShow = true;
                    }
                    else
                    {
                        foreach (string field in searchFields)
                        {
                            if (field.RemoveRichTags().ToLower().Contains(searchLower))
                            {
                                shouldShow = true;
                                break;
                            }
                        }
                    }
                }

                if (shouldShow)
                {
                    visibleTiles.Add(tile);
                }
                else
                {
                    tile.gameObject.SetActive(false);
                }
            }

            // Update positions
            int planetY = Mathf.RoundToInt(ADOBase.controller.chosenPlanet.transform.position.y);
            for (int i = 0; i < visibleTiles.Count; i++)
            {
                CustomLevelTile tile = visibleTiles[i];
                tile.gameObject.SetActive(true);
                tile.transform.MoveY(planetY - i);
            }

            // Update level list objects
            var updateMethod = typeof(scnCLS).GetMethod("UpdateLevelListObjects", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMethod?.Invoke(instance, new object[] { visibleTiles });

            // Select or clear
            if (visibleTiles.Count != 0 && _pendingSelect)
            {
                var selectMethod = typeof(scnCLS).GetMethod("SelectLevel", new[] { typeof(CustomLevelTile), typeof(bool) });
                selectMethod?.Invoke(instance, new object[] { visibleTiles[0], true });
            }
            else
            {
                var displayMethod = typeof(scnCLS).GetMethod("DisplayLevel", BindingFlags.NonPublic | BindingFlags.Instance);
                displayMethod?.Invoke(instance, new object?[] { null });

                var stopSongMethod = typeof(scnCLS).GetMethod("StopCurrentLevelSong", BindingFlags.NonPublic | BindingFlags.Instance);
                stopSongMethod?.Invoke(instance, null);
            }

            // Update search text
            string searchDisplay = RDString.Get("cls.shortcut.find", null, LangSection.Translations);
            if (!string.IsNullOrEmpty(_pendingSearch))
            {
                searchDisplay += $" <color=#ffd000><i>{RDString.Get("cls.currentlySearching", new Dictionary<string, object> { { "filter", _pendingSearch } }, LangSection.Translations)}</i></color>";
            }

            // Update search text using reflection to avoid TMP_Text reference
            var currentSearchTextField = typeof(scnCLS).GetField("currentSearchText", BindingFlags.NonPublic | BindingFlags.Instance);
            if (currentSearchTextField != null)
            {
                var currentSearchText = currentSearchTextField.GetValue(instance);
                if (currentSearchText != null)
                {
                    var textProperty = currentSearchText.GetType().GetProperty("text");
                    textProperty?.SetValue(currentSearchText, searchDisplay);
                }
            }

            _searchCoroutine = null;
        }
    }
}
