# Changelog

개발 과정에서의 변경 사항과 설계 결정을 날짜별로 기록한다.
각 항목에 **왜** 이 변경을 했는지를 함께 남긴다.

---

## 2026-05-28

### Added
- `docs/analysis/` 폴더 신설 — AI 역설계 기반 아키텍처 분석 시리즈 (4개 항목)
- `docs/analysis/01_ui_components.md` — UI 화면 구성 초안 작성 (네비게이션, 메인, 레시피, 매뉴얼, 디바이스, 설정)
- `CHANGELOG.md` 추가

### Changed
- `README.md` 포트폴리오 형식으로 전면 재작성 — 프로젝트 개요·목적·환경·빌드 명령 구성
- `README.md` 솔루션 구조에 `Tcd.Plc.Simulator` 추가
- `CLAUDE.md` 솔루션 구조에 `Tcd.Robot.Simulator`, `Tcd.Plc.Simulator` 누락 항목 보완

### Why
- README가 빌드 명령 나열에 그쳐 포트폴리오로서 맥락이 부족했음
- 분석 문서를 GitHub에 직접 올려 면접관이 링크로 바로 접근할 수 있도록 구조화
- CHANGELOG는 날짜별 설계 의사결정을 추적하기 위해 도입

---

## 2026-05-27

### Added
- `Device` 네비게이션 페이지 — SPiiPlus·Robot·PLC 탭으로 하드웨어 연결 통합 관리
- `DeviceSettings` 클래스 및 `device.json` — 하드웨어 연결 설정 독립 분리

### Changed
- `AppSettings`에서 하드웨어 연결 항목(SpiiPlus IP, Robot host 등) 제거
- `Manual` 페이지에서 Robot·PLC 탭 제거 → 상단 통신상태 바(LED) + Motor 탭만 유지
- `Settings` 페이지를 타임아웃·로그 경로 등 앱 환경설정 전용으로 축소

### Why
- `AppSettings` 하나에 환경설정과 디바이스 연결 설정이 섞여 책임이 불분명했음
- Manual 페이지는 "수동 제어" 목적인데 연결 관리 UI가 함께 있어 혼란스러웠음
- 연결 상태 조작은 Device 페이지, 확인만 Manual 상단 바로 역할을 명확히 분리

---

## 2026-05-20

### Added
- `Robot` 뷰 추가
- `Recipe` 파사드 패턴 적용 — `MainCore.Recipes`를 통한 단일 접근점

### Changed
- `SemiAutoAlignUVWSequence`에 `Task.WhenAll` 적용 — U·V·W 동시 이동 후 결과 통합 처리
- `AppSettings.UseSpiiPlus` 기본값 `false` 로 변경 — 시뮬레이터 환경에서 연결 실패 방지
- `MainWindowViewModel`에 `RunSequenceAsync` 헬퍼 추출 — 반복 패턴 제거

### Fixed
- SemiAuto·Auto 시퀀스의 `Param` 공개 필드 제거 — 파라미터는 `ExecuteAsync` 인자로만 전달
- `MainCore.Instance` 직접 참조 → 생성자 주입으로 교체 — 테스트 가능성 확보

### Why
- 시퀀스가 stateless여야 하는데 `Param` 필드가 상태를 가져 멀티스레드 환경에서 위험했음
- `MainCore` 싱글턴 직접 참조는 단위 테스트 작성을 불가능하게 만들었음
- AlignUVW는 세 축이 독립적으로 움직이므로 순차 대기가 불필요한 지연이었음
