> [!IMPORTANT]
> 若您在使用Iridium Beta版时出现问题，请及时向维护者报告。

## r22 beta6

### 变更

1. **新增编辑器砖块性能优化 (EditorFloorOptimization)**: 重写编辑器砖块插入/删除逻辑，跳过完整的 `RemakePath` 重建，采用增量式砖块操作。包含 9 个子补丁:
   - `incrementalFloorInsert` — 增量式砖块插入/删除（跳过完整路径重建）
   - `rangeBasedRedraw` — 范围式重绘（仅重绘受影响的砖块）
   - `skipRedundantRemakePath` — 跳过冗余的 RemakePath 调用
   - `optimizeOffsetFloorEvents` — 优化事件中的砖块 ID 偏移
2. **移除 `fixDspTimeCalibration` 设置项**: v2.10.0 原生已正确校准，不再需要该修复
3. **端口双分支支持**: EditorFloorOptimization 同时在 `main` (v2.9.8) 和 `frontline` (v2.10.0) 两个分支可用

### 修复

1. 修复: `JsonPatches.GetCustomLevelName` 反序列化失败（JSON 键顺序导致 `DeserializePartially` 找不到 `settings` 键），添加完整 `Deserialize` 回退
2. 修复: `MarginTracker` 来源字段引用错误
3. 修复: Ctrl+F 搜索功能优化，减少不必要的重新计算
4. 修复: `_coopMode` 字段引用错误
5. 修复: 增量插入后事件图标位置未更新（添加 `ApplyEventsToFloors` 调用）
