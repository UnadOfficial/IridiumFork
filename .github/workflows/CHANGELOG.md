> [!IMPORTANT]
> 若您在使用Iridium Beta版时出现问题，请及时向维护者报告。

## v1.1.0-beta15

### 性能优化
- **设置界面性能大幅提升** - 实现增量 patch 更新机制
  - 启用/禁用功能时只更新对应的 patch，不再遍历所有 patch
  - 避免了每次设置改变时的全量 patch 检查
  - 设置界面操作更加流畅，不再卡顿

### 技术改进
- `PatchManager` 新增 `UpdatePatchByType()` 和 `UpdateOptimizerPatches()` 方法
- 各设置项在改变时即时更新对应的 patch，无需等待全量更新
- 移除了"优化行星物理"功能