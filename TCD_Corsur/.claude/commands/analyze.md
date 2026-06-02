# 코드 분석 및 개선 제안

너는 TCD_Cursor 프로젝트의 코드 품질 분석가야. 아규먼트로 분석 대상 경로를 받아 코드 구조를 파악하고 개선 가능한 부분을 우선순위별로 제안해줘.

**아규먼트**: 분석할 폴더 또는 파일 경로 (예: `Sequences/SemiAuto`, `View/Manual`). 생략 시 솔루션 전체를 분석한다.

---

## 분석 순서

### 1. 대상 파일 수집
- 아규먼트가 있으면 해당 경로 아래 `.cs` 파일만 수집 (Glob)
- 생략 시 `Tcd.App`, `Tcd.Engine`, `Tcd.Simulator` 전체 수집
- 파일 수가 많으면 핵심 파일 위주로 샘플링

### 2. 구조 파악
- 각 파일의 클래스/인터페이스 목적과 역할 파악
- `CLAUDE.md`의 아키텍처 규칙 (의존성 방향, MVVM, Sequence 계층) 대조

### 3. 컨벤션 체크 (CLAUDE.md 기준)

**하드코딩 탐지**
- `Tcd.App/Define/` 상수 클래스를 거치지 않은 리터럴 문자열 키/ID
- `TcdSequenceKeys.cs`에 없는 시퀀스 키 문자열

**네이밍 · 스타일**
- PascalCase / camelCase / `_camelCase` 규칙 위반
- 2-space 들여쓰기 이탈 (눈에 띄는 경우만)

**비동기 패턴**
- 디바이스 호출에서 `async Task` 미사용
- UI 쪽 비-UI 코드에서 `ConfigureAwait(false)` 누락

**취소 패턴**
- `_activeCts?.Cancel(); _activeCts?.Dispose()` 패턴 미준수

### 4. 아키텍처 품질 체크

**의존성 방향 위반**
- `Tcd.Engine`이 `Tcd.App`을 참조하는 경우
- ViewModel이 `IMotionService`를 직접 호출 (SequenceManager 우회)

**단일 책임**
- 메서드 길이 60줄 초과 또는 명백한 다중 책임

**인터락 누락**
- `SPEC_Interlocks.md`에 명시된 인터락이 구현 코드에 없는 경우

### 5. 테스트 커버리지 체크
- `Tcd.Engine.Tests` / `Tcd.Simulator.Tests`에 대응 테스트가 없는 새 로직
- `CLAUDE.md`: "New behavior should have unit tests"

---

## 출력 형식

분석이 끝나면 아래 형식으로 출력한다. 항목이 없는 카테고리는 "없음"으로 표기.

```
## 분석 결과: [대상 경로 또는 "전체"]

### P0 — 즉시 수정 (빌드/런타임 위험)
- [파일:라인] 문제 설명 → 제안 방향

### P1 — 다음 작업 (컨벤션·아키텍처 위반)
- [파일:라인] 문제 설명 → 제안 방향

### P2 — 개선 아이디어 (선택적 리팩토링)
- [파일:라인] 문제 설명 → 제안 방향

### 테스트 누락
- [파일] 테스트가 없는 로직 설명

### 요약
전체 N개 파일 분석. 즉시 수정 X건, 컨벤션 위반 Y건, 개선 아이디어 Z건.
```

분석 후 "리팩토링을 GitHub 이슈로 등록하거나 구현 계획을 세울까요?" 라고 사용자에게 묻는다.
