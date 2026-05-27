> [!IMPORTANT]
> 若您在使用Iridium时出现问题，请及时向维护者报告。

## Iridium 1.2.1 (r22)

### 功能新增

1. **暂停菜单行星轨迹保留**: 暂停菜单打开时跳过 `DisableParticles` 调用，保留行星运动轨迹线
2. **判定文本字数上限提升至 128**: 支持富文本标签（如 `<color=red>`）
3. **VRAM 加载停止按钮**: 帧扩散装饰加载界面增加停止按钮，可在加载过程中手动中断
4. **编辑器砖块性能优化 (EditorFloorOptimization)**: 重写编辑器砖块插入/删除逻辑，跳过完整的 `RemakePath` 重建，采用增量式砖块操作:
   - 增量式砖块插入/删除（跳过完整路径重建）
   - 范围式重绘（仅重绘受影响的砖块）
   - 跳过冗余的 RemakePath 调用
   - 优化事件中的砖块 ID 偏移
5. **AsyncInput 启动快照始终启用**: 将启动偏移校准从编辑器修复中分离，确保每次关卡开始都进行快照校准
6. **EventTween 缓存优化**: 缓存 `eventTweens` getter 结果，优化 ffx 移动/变色特效性能
7. **自定义关卡 JSON 读取优化**: 使用 `DeserializePartially` 替代完整反序列化，提升读取性能

### 修复

1. 修复: `FrameSpreadDecorationLoading` 异步加载期间拦截 `Play`/`ResetDecorations`，添加 `CleanupState` 状态清理和加载进度文本
2. 修复: `EditorFloorOptimizationPatches` — 改用 `RemakePath` 管线，修复残留 ffx 清理和重复 `ResetFloorState` 调用
3. 修复: v2.10.0 `scrPlayer.marginTracker` 改为只读属性，移除已废弃的 `MarginTrackerSetPlayerCountFix` 和 `MarginTrackerResetFix` 补丁（由游戏原生处理）
4. 修复: 编辑器砖块增量插入后事件图标位置未更新（添加 `ApplyEventsToFloors` 调用）
5. 修复: 编辑器砖块性能优化在 v2.9.8 和 v2.10.0 双分支可用
6. 修复: `JsonPatches.GetCustomLevelName` 反序列化失败（JSON 键顺序导致 `DeserializePartially` 找不到 `settings` 键），添加完整 `Deserialize` 回退
7. 修复: `MarginTracker` 来源字段引用错误
8. 修复: Ctrl+F 搜索功能减少不必要的重新计算
9. 修复: `_coopMode` 字段引用错误
10. 修复: margin tracker 在进入编辑器时残留，以及水印开关问题
11. 修复: `TurnaroundConditionFix` — 匹配 v2.9.8 hairpin 检测
12. 修复: 移除已废弃的 `RTMaxSizePatch`，添加 v2.10.0 三个 bugfix 补丁（twirl 符号、AddHit 速度、编辑器重置）
13. 修复: `HitTextMeshShowRotationFixPatch` 移至 BugfixPatches
14. 修复: 还原 v2.10.0 移除的 AsyncInputManager 每帧 dspTime 校准
15. 修复: `MoveDecoration` 图片预加载阻塞主线程
16. 修复: vanilla v2.10.0 非合作模式 `missAngle` 未转发至 `Show`
17. 修复: `EditorPlayResetMistakesPatch` 独立的开关选项
18. 修复: 纹理压缩中保留 Alpha 通道（`HasAlphaPixels` 检查）
19. 修复: `getLastReleaseTag` 使用版本排序导致跨 release 系列的 tag 干扰，改为 `git describe` 回溯
20. 修复: Release 工作流浅克隆导致只拉取 1 个 commit，提交记录只显示一条
21. 修复: Release 预览不再显示全部历史 commit，改为只展示两次 release 之间的提交
22. 修复: Release tag 名称统一格式

### 优化与重构

1. **内存清理重构**: 将内存清理触发时机重构为场景切换触发
2. **代码结构优化**: 重新组织 bugfix 补丁分类，新增 dspTime 校准开关
3. **EventTween 缓存**: 缓存 ffx move/recolor 事件的 `eventTweens` getter 结果
4. **帧扩散装饰加载优化**: 为 v2.10.0 优化帧扩散装饰加载逻辑

### 杂项

1. 移除 `fixDspTimeCalibration` 设置项（v2.10.0 原生已正确校准，不再需要）
2. 双分支支持: 所有功能同时在 `main` (v2.9.8) 和 `frontline` (v2.10.0) 两个分支可用
