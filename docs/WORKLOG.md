# Work Log — TCD_Cursor

`/done` 커맨드가 자동으로 업데이트한다. 최신 항목이 위에 쌓인다.

---

## 2026-05-27

### 변경 내용
- [Core] DeviceSettings.cs 신규 — 하드웨어 연결 설정(SPiiPlus·Robot·PLC) device.json 영속화 분리
- [Core] MainCore — Devices 프로퍼티 추가, AppSettings에서 하드웨어 항목 제거, 로그경로 필드 추가
- [App/UI] View/Device/ 신규 — DeviceViewModel + DeviceView (SPiiPlus·Robot·PLC 탭 통합 연결 관리 페이지)
- [App/UI] SettingsViewModel/View 재작성 — 타임아웃·로그경로 환경설정 전용으로 변경
- [App/UI] ManualViewModel/View — Robot·PLC 탭 제거, 상단 통신상태 바 + Motor 탭만 유지
- [App/UI] Device_PlcViewModel/View 전면 구현 — Host·Port·Connect·Disconnect + device.json 연동
- [App/UI] Device_Spii/Robot/PlcViewModel — Settings.* → Devices.* 전환, 저장 시 Save() 호출
- [App/UI] MainWindowViewModel — DeviceViewModel 프로퍼티, Cmd_ShowDevicePage·Cmd_ShowSettingsPage 추가
- [App/UI] MainWindow.xaml — Device·Settings 네비게이션 버튼 및 DeviceViewModel DataTemplate 등록

### 브랜치 / 커밋
- Branch: refactor/device-nav-separation
- Commit: 6ba8442 — refactor(core,ui): DeviceSettings 분리 및 Device 네비게이션 페이지 추가

---

## 2026-05-26

### 변경 내용
- [App/UI] Robot View 추가 — TCP 로봇 시뮬레이터 Connect/Disconnect, 11개 위치 이동 버튼, 상태 LED
- [App/UI] MainWindow 하단 네비게이션에 Robot 탭 버튼 및 DataTemplate 추가
- [App/UI] MainWindowViewModel — RobotViewModel 프로퍼티 추가, 기존 `Robot` string → `RobotStatus` rename
- [Styles] RobotStyles.xaml 추가 (Conn/Run/Error LED, PosButton 스타일)
- [Core] MainCore.SaveRecipe / LoadRecipe 파사드 추가 — 레시피 저장·불러오기 일관성 보장
- [App/UI] RecipeViewModel 리팩토링 — 커맨드 익명 함수 → OnCmd_* 명명 메서드로 분리
- [Docs] CODING_CONVENTIONS.md 작성 — 커맨드/프로퍼티/생성자 컨벤션 규칙 3개
- [Docs] WORKLOG.md, Recipe-architecture.md 초기 파일 생성
- [Config] /done 슬래시 커맨드 추가 — 작업 마무리 자동화
- [Test] Tcd.Engine.Tests, Tcd.Simulator.Tests, Tcd.Tests.Shared 프로젝트 추가 (솔루션 등록)
- [Build] TCD_Corsur.sln 업데이트 — 신규 프로젝트 4개 등록

### 브랜치 / 커밋
- Branch: develop/SettingUI
- Commit: 5b79770 — feat(app): Robot 뷰 추가 및 Recipe 파사드 패턴 적용

---
