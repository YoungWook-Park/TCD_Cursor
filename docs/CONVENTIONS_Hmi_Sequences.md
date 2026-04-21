# CONVENTIONS: HMI·시퀀스 네이밍 및 공통 규칙

| 항목 | 내용 |
|------|------|
| 목적 | `Vcd.Hmi.Wpf`·시퀀스 관련 코드/데이터를 **일관되게** 유지해 유지보수와 현장 디버깅을 쉽게 한다. |
| 실행 백로그 | [PLAN_Hmi_Sequence_Execution.md](PLAN_Hmi_Sequence_Execution.md) |
| UI 동작 스펙 | [SPEC_UI_Hmi.md](SPEC_UI_Hmi.md) |
| 시퀀스 동작 스펙 | [SPEC_Sequence_Engine.md](SPEC_Sequence_Engine.md) |

---

## 1. View / ViewModel

- **파일 쌍**: `FooView.xaml` + `FooView.xaml.cs` + `FooViewModel.cs` (필요 시 partial은 동일 접두).
- **네이밍**: 화면 클래스는 `*View`, 해당 ViewModel은 `*ViewModel` (예: `DeviceView` / `DeviceViewModel`, `Monitor3DView` / `Monitor3DViewModel`).
- **네임스페이스**: 폴더 구조와 맞춘다 (예: `Vcd.Hmi.Wpf.Views.Manual.Motor`).
- **런타임 `DataContext`**: 부모·`DataTemplate`로 주입; **디자인 타입**은 XAML `d:DesignInstance`로 별도 지정한다.
- **디자인 크기(풀HD 기준)**: 메인 창 `1920×1080`; 본문 영역(제목줄 40px·하단 내비 56px 제외)은 **`d:DesignHeight="984"`** 와 `d:DesignWidth="1920"`로 탭용 `UserControl`을 맞춘다. 매뉴얼 하위 패널은 상단 서브바를 고려해 대략 **`1880×900`**. 장비 상태 배너는 **`1920×120`**. 보조 창(로그 뷰어 등)은 별도 지정.
- **폴더(현재 구조)**: `Views/Main/`(예: `EquipmentStatusView`), `Views/Device/`, `Views/Recipe/`, `Views/Teach/`, `Views/Monitor/`, `Views/Manual/` 및 하위 `Motor/`, `Robot/`, `Plc/`. ViewModel은 해당 View와 **같은 기능 폴더**에 둔다. 별도 `ViewModels/` 루트 폴더는 사용하지 않는다.

---

## 2. 메인 셸

- **창**: `MainWindow`.
- **ViewModel**: `MainWindowViewModel` (셸 전용; 하위 탭용 `*ViewModel`과 구분).
- **위치 권장**: `MainWindowViewModel.cs`는 `MainWindow`와 같은 어셈블리 루트(`Vcd.Hmi.Wpf`)에 두어 탐색을 쉽게 한다.

---

## 3. 시퀀스 ID (C#)

- **그래프 정의 id**와 **등록된 스텝 id**는 문자열 리터럴을 코드 곳곳에 쓰지 않는다.
- 단일 출처: `Vcd.Contracts.Define.SequenceDef`의 `public const string` (구현은 [PLAN_Hmi_Sequence_Execution.md](PLAN_Hmi_Sequence_Execution.md) 단계 A/A′).
- **새 id 추가 체크리스트**
  1. `sequences/**/*.json`의 `"id"` 또는 `step` 노드 문자열
  2. `SequenceDef`에 동일 문자열 상수 추가
  3. `ISequenceStep` 구현체의 `Id` 프로퍼티 (해당 시)
  4. 호출부 `SequenceManager.RunAsync` / `ExecuteDefinitionAsync` / HMI `SequenceHostController.StartAsync` 등

JSON은 런타임 데이터이므로 **파일 안의 문자열은 수동으로** 상수와 맞춘다.

**HMI 호스트 실행**: `SequenceHostController.StartAsync(SequenceDef.xxx)`로 시작하고, 호출자는 `await`로 `SequenceRunResult`를 받는다. 중단은 `RequestStop()` (내부 `CancellationToken` 취소).

---

## 4. 수동(Manual) 시퀀스

- **정의 id**: `Manual_` 접두 (예: `Manual_AbsMove`).
- **폴더**: `sequences/manual/` 아래에 `motor`, `robot`, `plc` 등 **기능 단위** 하위 폴더를 둔다.
- C#에서 참조할 이름은 항상 **`SequenceDef`**에 추가한다.

---

## 5. ViewModel 구현 스타일 (장비 HMI)

- **공통 의존성**: `App.Dialogs`, `App.Plc`, `App.History.Enabled` 등 반복 참조는 **생성자 또는 `InitXxx()`에서 readonly/필드에 대입**하고, 메서드 본문에서는 그 필드만 사용한다.
- **바인딩 프로퍼티**: `get` / `set` 블록 + `SetProperty(ref _backing, value)` (CommunityToolkit `ObservableObject`). 생성기 `[ObservableProperty]` 대신 **명시 프로퍼티**를 쓰면 중단점·F12 추적이 수월하다는 팀 기준을 따른다.
- **생성자**: 한 메서드에 모든 로직을 넣지 않고, `InitChildViewModels()`, `InitFromAppSettings()` 등 **private 초기화 메서드**만 호출하는 파사드 형태로 둔다.
- **`#region` 권장 구분**: `Variable`, `Construction / initialization`, `Bindable properties`, `Commands`, `PLC / equipment sync`, `UI helpers` 등 실제 코드베이스에 맞게 통일.
- **명령**: `CommunityToolkit.Mvvm`의 `[RelayCommand]` 사용은 유지해도 된다.

---

## 6. 서드파티 UI

- **DevExpress Navigation Bar**: 현재 **채택 보류** (라이선스·스택). 하위 메뉴는 WPF `TabControl`, 토글 버튼 + `ContentControl`, `DataTemplate`로 구성한다. [TECH_STACK.md](TECH_STACK.md)의 optional 표기와 충돌 없음.

---

## 7. 문서 역할 구분

| 접두/이름 | 역할 |
|-----------|------|
| `PLAN_*` | **무엇을** 어떤 순서로 할지 (백로그). |
| `CONVENTIONS_*` | **어떻게** 이름 짓고 짤지 (규약). |
| `SPEC_*` | **무엇이 맞는 동작/아키텍처인지** (요구사항). |
| `ROADMAP.md` | 페이즈별 목표와 SPEC 링크. |

---

## Change History

| Date | Summary |
|------|---------|
| 2026-04-01 | Initial conventions from HMI·sequence documentation baseline. |
