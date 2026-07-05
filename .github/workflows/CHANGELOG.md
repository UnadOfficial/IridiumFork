## 1.4.0_beta1

> [!Info]
> Iridium 从 1.3.0_beta1 开始就已经支持 MelonLoader，有关 Iridium 在 MelonLoader 上的安装，请查阅 README.md

### 新增 / New Features / 새로운 기능

- **自定义判定文本与偏移显示可以同时使用**：旧版只能二选一（要么自定义文字，要么显示偏移值），现在在自定义文本中加入 `{offset}` 即可同时显示自定义文字和偏移
- **Custom judge text and offset display can now work together**: Previously you had to choose one (either custom text OR show offset); now just add `{offset}` in your custom text to have both
- **사용자 정의 판정 텍스트와 오프셋 표시를 동시에 사용 가능**: 이전에는 둘 중 하나만 선택해야 했지만(커스텀 텍스트 또는 오프셋 표시), 이제 커스텀 텍스트에 `{offset}`을 추가하여 둘 다 표시할 수 있습니다

- **一键转换为偏移格式**：在判定文本设置中点击"转换为偏移格式"，所有文本自动替换为 `{offset}ms`
- **Convert all to offset**: One-click button in judge text settings to replace all texts with `{offset}ms`
- **일괄 오프셋 변환**: 판정 텍스트 설정에서 "오프셋 형식으로 변환" 버튼 클릭 시 모든 텍스트가 `{offset}ms`로 변경됩니다

### 改进 / Improvements / 개선

- **判定文本立即生效**：修改判定文本后，无需重启游戏，下一拍立即显示新文本
- **Instant judge text updates**: Changes to judge text take effect on the next hit, no restart needed
- **판정 텍스트 즉시 적용**: 판정 텍스트 수정 후 게임 재시작 없이 다음 타격부터 새 텍스트가 표시됩니다

> [!Tip]
> 留空 = 不显示该判定文字 / Leave empty = hide this judge text / 비워두면 해당 판정 텍스트가 표시되지 않습니다

### 修复 / Bug Fixes / 버그 수정

- **关闭自定义判定文本后仍显示自定义文字**：现在关闭后正确显示游戏默认判定文本
- **Disabled judge text customization still showed custom text**: Now correctly shows default game text when disabled
- **판정 텍스트 사용자 정의 비활성화 시에도 커스텀 텍스트 표시되던 문제**: 비활성화 시 게임 기본 텍스트로 올바르게 표시됩니다

- **判定文本旋转修复**：修复单人模式下判定文本不旋转的问题
- **Judge text rotation fix**: Fixed judge text not rotating in single player mode
- **판정 텍스트 회전 수정**: 싱글 플레이 모드에서 판정 텍스트가 회전하지 않던 문제 수정

---
> [!NOTE]
> **感谢各位 Iridium 使用者的反馈，Iridium 成功进入 Beta 阶段！**
> **Thanks to all Iridium users for your feedback — Iridium has entered Beta!**
> **모든 Iridium 사용자 여러분의 피드백 덕분에 Iridium이 Beta 단계에 진입했습니다!**

### 本地化 / Localization / 지역화

- **韩语支持**：判定文本设置相关翻译已添加韩语支持
- **Korean language support**: Added Korean translations for judge text settings
- **한국어 지원**: 판정 텍스트 설정 관련 한국어 번역 추가

- **简体中文支持**：判定文本设置相关翻译已更新
- **Simplified Chinese support**: Updated Chinese translations for judge text settings
- **간체 중국어 지원**: 판정 텍스트 설정 관련 중국어 번역 업데이트
