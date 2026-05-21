> [!IMPORTANT]
> 若您在使用Iridium Beta版时出现问题，请及时向维护者报告。

## r22 beta4

### 变更

1. 重构内存清理机制: 移除基于定时器的 `SmartGCPatch`，改为**切换场景时自动清理** (`scnGame.OnDestroy`)
2. 设置项简化: 移除 `GC Interval` / `GC during gameplay`，替换为 `Clean on Scene Change` 单一开关
3. 协程宿主迁移至 `DontDestroyOnLoad` 的 `VRAMNotificationUI`，避免场景销毁时无法执行异步清理

### 修复

1. 修复: 进入编辑器时结算异常累积 (`scrMistakesManager.Reset` 无法清除旧 `marginTracker` 数据) (frontline v2.10.0)
2. 修复: Beta 水印开关设置不生效，现在开关可正确显示/隐藏水印 (main + frontline)
3. 更新: v2.10.0 的 `Assembly-CSharp.dll`
