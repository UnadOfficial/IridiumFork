## r41_nightly3

### 改进 / Improvements / 개선

- **IML 界面引擎重写**：移植设置面板所有页面到 IML 格式，支持内联样式、Flex 布局、条件渲染，后续 UI 开发更灵活
- **IML UI engine rewritten**: All settings pages migrated to IML format with inline styles, Flex layout, and conditional rendering for more flexible future UI development
- **IML UI 엔진 재작성**: 모든 설정 페이지를 IML 형식으로 마이그레이션, 인라인 스타일, Flex 레이아웃, 조건부 렌더링 지원으로 향후 UI 개발 유연성 향상

- **VRAM 通知 UI 重写**：从旧版 IMGUI 迁移到新渲染器，修复某些情况下进度条卡在 0/129 的问题
- **VRAM notification rewritten**: Migrated from legacy IMGUI to the new renderer; fixed progress bar stuck at 0/129 in some scenarios
- **VRAM 알림 UI 재작성**: 레거시 IMGUI에서 새 렌더러로 마이그레이션, 일부 상황에서 진행률이 0/129에 멈추는 문제 수정

### 修复 / Bug Fixes / 버그 수정

- **判定文本设置可正常编辑**：修复设置界面中自定义判定文本输入框无法输入和退格删除的问题
- **Judge text setting now editable**: Fixed custom judge text input fields not accepting keyboard input or backspace
- **판정 텍스트 설정 편집 가능**: 사용자 정의 판정 텍스트 입력 필드에서 키보드 입력 및 백스페이스가 작동하지 않던 문제 수정

- **新特性弹窗显示优化**：修复 IML 化后的首次运行弹窗和升级提示窗口不显示的问题
- **First-run/upgrade dialog visibility**: Fixed the welcome and upgrade dialogs not appearing after the IML migration
- **신규 기능 팝업 표시 최적화**: IML 마이그레이션 후 첫 실행 팝업 및 업그레이드 알림 창이 표시되지 않던 문제 수정
