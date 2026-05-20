# 개발 가이드라인

프로젝트 공통 규칙. 상세 코드 구조는 [CLAUDE.md](../TCD_Corsur/CLAUDE.md) 참고.

---

## 코드 스타일

- 들여쓰기: **2 스페이스**
- 줄 길이: **80자 이하** 권장
- 네이밍: C# 표준 (타입·메서드 PascalCase, 로컬 camelCase, 필드 _camelCase)
- 비동기: 디바이스 조작은 `async Task`; UI 제외 모든 곳에 `ConfigureAwait(false)`
- 취소: `_activeCts?.Cancel(); _activeCts?.Dispose()` — 새 시작 전 항상 이전 취소
- 스레드: UI 업데이트는 `App.Current.Dispatcher.Invoke()`

## 설계 원칙

- **No magic strings**: 모든 ID/키는 `Define/` 하위 상수 클래스에 선언
  - 시퀀스 키 → `TcdSequenceKeys.cs`
  - 알람 코드 → `AlarmKeys.cs`
  - 로봇 위치명 → `RobotDefine.cs`
- **단방향 의존**: `Tcd.App` → `Tcd.Engine` ← `Tcd.Simulator` (역방향 금지)
- **인터락**: `throw InvalidOperationException`으로 조건 위반 표시; `DelegateSequence`가 Alarm + Fail로 변환

## 테스트

- 새 동작에는 단위 테스트 추가
- 시퀀스 엔진 테스트: 실제 디바이스 I/O 불필요, `TcdSimulation`으로 인프로세스 검증

## 버전 관리

- 기능 브랜치 사용, main은 항상 빌드 가능 상태 유지
- 커밋 메시지: 변경 이유(why) 위주로 작성
