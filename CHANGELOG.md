# Iridium Changelog

## r31_prerelease1

### 自定义缓速引擎 Bug 修复 / Custom Easing Engine Bug Fixes / 사용자 정의 감속 엔진 버그 수정

- **装饰物缩放互斥修复**：ScaleX/ScaleY 各自独立 float tween，每帧从 `dec.scaleVec` 实时读取另一轴当前值，避免两轴互相覆盖（等效原版 AxisConstraint）
- **装饰物旋转修复**：`r => dec.rotAngle = r` → `r => dec.SetRotation(r)`，每帧同步至 transform，不再仅在完成时跳转
- **装饰物位置/视差偏移/枢轴修复**：`PositionX/Y`、`ParallaxOffsetX/Y`、`PivotX/Y` 改为每帧调用对应 `Set*` 方法，动画期间视觉正确更新
- **轨道旋转修复**：float tween 每帧同时写 `target.tweenRot.z` 和 `tform.eulerAngles`，匹配原版 DOTween OnUpdate 行为
- **轨道缩放修复**：从 Vector3 tween 改为 float tween 分别控制 X/Y 轴，实时读取另一轴，保留 Z 轴原值

- **Decoration scale fix**: Split into independent float tweens for X/Y; each reads the other axis live from `dec.scaleVec` to prevent fighting (equivalent to original AxisConstraint)
- **Decoration rotation fix**: `r => dec.rotAngle = r` → `r => dec.SetRotation(r)`, synced to transform every frame instead of snapping on completion
- **Decoration position/parallaxOffset/pivot fix**: `PositionX/Y`, `ParallaxOffsetX/Y`, `PivotX/Y` now call the respective `Set*` methods each frame for correct visual updates during animation
- **Track rotation fix**: Float tween writes both `target.tweenRot.z` and `tform.eulerAngles` each frame, matching original DOTween OnUpdate behavior
- **Track scale fix**: Changed from Vector3 tweens to per-axis float tweens reading the other axis live, preserving original Z value

- **장식 스케일 수정**: ScaleX/ScaleY를 독립 float tween으로 분리, 각각 `dec.scaleVec`에서 실시간으로 다른 축 값을 읽어 충돌 방지 (원본 AxisConstraint와 동일)
- **장식 회전 수정**: `r => dec.rotAngle = r` → `r => dec.SetRotation(r)`, 완료 시 스냅 대신 매 프레임 transform에 동기화
- **장식 위치/시차오프셋/피벗 수정**: `PositionX/Y`, `ParallaxOffsetX/Y`, `PivotX/Y`가 매 프레임 해당 `Set*` 메서드를 호출하여 애니메이션 중 시각적 업데이트 정상화
- **트랙 회전 수정**: float tween이 매 프레임 `target.tweenRot.z`와 `tform.eulerAngles`를 함께 기록, 원본 DOTween OnUpdate 동작 일치
- **트랙 스케일 수정**: Vector3 tween에서 축별 float tween으로 변경, 실시간으로 다른 축 값을 읽고 Z 값 유지

### 新增 / New / 신규

- **自定义缓速引擎**：新增 `enableCustomEasingEngine` 设置，使用轻量级 struct-based IrTween 替代 DOTween 处理 MoveTrack/RecolorTrack/MoveDecoration，零 GC 分配、对象池管理
- **Mutual exclusion**: Auto-disables conflicting optimize options (`optimizeMoveTrack`/`optimizeRecolorTrack`/`optimizeMoveDecorations`) when custom easing engine is enabled
- **상호 배제**: 사용자 정의 감속 엔진 활성화 시 충돌하는 최적화 옵션(`optimizeMoveTrack`/`optimizeRecolorTrack`/`optimizeMoveDecorations`) 자동 비활성화

