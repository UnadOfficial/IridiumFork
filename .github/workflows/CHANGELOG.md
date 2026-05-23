> [!IMPORTANT]
> 若您在使用Iridium Beta版时出现问题，请及时向维护者报告。

## r22 beta5

### 变更

1. **新增 `fixDspTimeCalibration` 设置项** (Compatibility 选项卡): v2.10.0 去掉了每帧的 `AsyncInputManager.offsetTick` 重算，异步输入的时间基准不再与音频时钟对齐。新增的修复每帧检测偏移并重新校准，消除长期游玩的判定偏移累积
2. **JPEG 压缩保留透明度**: 装饰物大图（>5MB）压缩时新增 `HasAlphaPixels` 检测，有透明像素的图片强制存为 PNG 而非 JPEG，修复透明度丢失
3. **VRAM 通知界面新增停止按钮**: 支持在分帧装饰物加载过程中手动取消
4. **Bugfix 补丁整理**:
   - `HitTextMeshShowRotationFixPatch` 移入 `BugfixPatches` 统一管理
   - `EditorPlayResetMistakesPatch` 拥有独立开关，不再绑定到位移显示
5. **装饰物日志增强**: 分帧加载时记录关卡路径和前 10 个装饰物信息，便于调试

### 修复

1. 修复: vanilla v2.10.0 `non-coop` 模式下 `missAngle` 未正确传递到 `Show` 方法 (BugfixPatches)
2. 修复: `MoveDecoration` 图片预加载阻塞主线程，改为逐帧 yield 而非整批处理
3. 更新: v2.10.0 的 `Assembly-CSharp.dll`
