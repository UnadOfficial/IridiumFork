### 新增

1. MelonLoader 下通过 `Ctrl+F9` 快捷键开关设置面板，面板居中显示并支持滚动
2. 支持自定义快捷键字符串，编辑 Settings.xml 中 `<panelToggleHotkey>` 即可（如 `Ctrl+Shift+F1`）
3. 设置加载路径改为基于程序集位置，修复加载器路径不同步导致设置读取失败的问题

### 改进

1. 修复 MelonLoader 下设置全为默认值（Disabled）的问题
2. 修复 CopyToOut 中 UMM Loader 缺少 Exists 条件导致 CI 构建失败的问题
