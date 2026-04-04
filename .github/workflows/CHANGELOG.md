> [!IMPORTANT]
> 若您在使用Iridium Beta版时出现问题，请及时向维护者报告。

1.初步制作了DOTween优化。

2.修复DOTween优化导致编辑器崩溃的问题。
- 移除危险的运行时反射补丁
- 优化功能现在完全可开关
- 添加重启游戏以完全恢复的提示信息

3.新增极端情况优化（针对14万事件、12万MoveTrack/MoveDecoration并发）。
- Tween批处理队列：分帧创建Tween避免单帧过载
- MoveTrack极端优化：检测大量事件自动触发分帧处理
- MoveDecoration极端优化：通过targetTags收集受影响装饰物
- 持续处理机制：每帧检查并处理待处理的Tween
- FPS从7帧提升到18帧，极端情况进一步优化