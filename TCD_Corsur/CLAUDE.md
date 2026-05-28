# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Overview

**TCD_Cursor** is a WPF-based factory automation HMI simulator for **Thermal Compression Bonding** (TC Bonding / Lamination) equipment. It is a portfolio project demonstrating end-to-end manufacturing software: HMI, sequence engine, motion control, PLC I/O, logging, and process history.

- PRD: `../docs/PRD_Lamination_Simulator.md`
- Roadmap: `../docs/ROADMAP.md`
- Class Design: `../docs/CLASS_DESIGN.md`

---

## Build Commands

```bash
# Build entire solution
dotnet build TCD_Corsur.sln

# Build a single project
dotnet build Tcd.App/Tcd.App.csproj

# Run the WPF application
dotnet run --project Tcd.App/Tcd.App.csproj
```

```bash
# Run unit tests
dotnet test Tcd.Engine.Tests/Tcd.Engine.Tests.csproj
dotnet test Tcd.Simulator.Tests/Tcd.Simulator.Tests.csproj
```

---

## Solution Structure

Production: five projects with strict one-way dependency:

```
Tcd.App  (net8.0-windows, WPF EXE)
  ├── Tcd.Engine  (netstandard2.0, domain library)
  └── Tcd.Simulator  (netstandard2.0, in-process device simulation)
       └── Tcd.Engine

Tcd.Robot.Simulator  (net8.0, standalone TCP robot simulator server)
Tcd.Plc.Simulator    (net8.0, standalone TCP PLC simulator server)
```

Test projects (no WPF dependency):

```
Tcd.Tests.Shared  (netstandard2.0, shared fakes)
  ├── Tcd.Engine.Tests  (net8.0, xUnit)
  └── Tcd.Simulator.Tests  (net8.0, xUnit)
```

**Tcd.Engine** — pure domain, no external deps
**Tcd.Simulator** — wraps Engine for in-process hardware sim
**Tcd.App** — WPF UI + composition root; references both
**Tcd.Robot.Simulator** — independent TCP server process simulating robot hardware
**Tcd.Plc.Simulator** — independent TCP server process simulating PLC hardware
**Tcd.Tests.Shared** — `FakeTimeProvider`, `FakeAlarmSink`, `FakeSequenceContext`, `BlockingFakeTimeProvider`

External SDK: `ACS.SPiiPlusNET.dll` in `/Dll/` (motion hardware)

---

## Architecture

### Composition Root: `MainCore` (singleton)

`Tcd.App/Core/MainCore.cs` is the single wiring point. It:
- Creates `TcdSimulation`, `IMotionService`, `IAxisStateProvider`
- Builds `SequenceManager` via `TcdSequenceRegistry.Build()`
- Registers Manual / Semi-Auto / Auto sequences
- Starts `LogWriter` (async batched sink)

**Hardware vs Simulator**: toggled by `AppSettings.UseSpiiPlus`. When `false`, `SimMotionService` wraps the in-process `TcdSimulation`; when `true`, `SpiiPlusMotionService` connects to the real ACS controller at `AppSettings.SpiiIpAddress`.

### Sequence System

All operations run through `SequenceManager.RunAsync(key, context, parameter, ct)`.

Hierarchy (from atomic to composite):
1. **Atomic sequences** — registered in `Tcd.Simulator/TcdSequenceRegistry.cs` (robot moves, axis commands, material ops, delays)
2. **Manual sequences** — 8 per axis (AbsMove, IncMove, JogMove, Stop, Home, FaultReset, ServoOn, ServoOff), factories in `Tcd.App/Sequences/Manual/Manual_Axis*.cs`
3. **Semi-Auto sequences** — `Tcd.App/Sequences/SemiAuto/`, compose atomic sequences
4. **Auto sequence** — `Tcd.App/Sequences/Auto/AutoRunSequence.cs`, orchestrates full cycle

All sequence keys are constants in `Tcd.Simulator/TcdSequenceKeys.cs`.

`DelegateSequence` wraps async lambdas: catches `OperationCanceledException` → `Stopped`; catches exceptions → raises `Alarm`, returns `Fail`.

### Motion Abstraction

```
IMotionService       — commands (AbsMove, IncMove, Jog, Stop, Home, ServoOn/Off, FaultClear)
IAxisStateProvider   — read-only state snapshots
AxisState            — Position, IsMoving, IsFault, IsHome, IsServoOn, IsLimitPos, IsLimitNeg
```

Axis index mapping (consistent everywhere): U=0, V=1, W=2, ZLower=3, ZUpper=4
Defined in `Tcd.App/Core/Define.cs` → `AxisDefine`

### MVVM Patterns

- `NotifyPropertyChangedBase` — `Set<T>(ref field, value)` raises only on change
- `RelayCommand` — simple `ICommand`, canExecute always true
- `BiRelayCommand` — adds optional canExecute predicate + `RaiseCanExecuteChanged()`
- All ViewModels in `Tcd.App/View/**/*ViewModel.cs`

### UI Navigation

`MainWindowViewModel` holds `CurrentContent` (object). Navigation commands swap it between `Main` (self), `Recipe` (RecipeViewModel), and `Manual` (ManualViewModel). The main window `ContentPresenter` renders whichever is current.

### Equipment Layout (HMI / Diagram)

Plan view (top-down):
- **Left side**: Upper Chamber (ZUpper axis, top-left), Lower Chamber assembly (ZLower + UVW axes, bottom-left). Each chamber is an **independent motor module** — no shared Z track between them.
- **Right side**: Robot (top-right), Stage 1 / Stage 2 (bottom-right)

Bonding animation: Upper Chamber moves ↓ and Lower Chamber moves ↑ independently when `IsBonding = true`.

---

## Key Files

| File | Role |
|------|------|
| `Tcd.App/Core/MainCore.cs` | Composition root, singleton |
| `Tcd.Simulator/TcdSequenceRegistry.cs` | All atomic sequence registrations |
| `Tcd.Simulator/TcdSequenceKeys.cs` | Sequence key constants |
| `Tcd.Engine/Sequences/SequenceManager.cs` | Sequence execution engine |
| `Tcd.Engine/Devices/MotionServiceContracts.cs` | `IMotionService`, `AxisState`, etc. |
| `Tcd.App/Core/Define.cs` | `AxisDefine`, `SpiiDefine` constants |
| `Tcd.App/MainWindowViewModel.cs` | Top-level ViewModel |
| `Tcd.App/View/Manual/Manual_MotorViewModel.cs` | Manual axis control ViewModel |
| `Tcd.Simulator/SimAxis.cs` | In-process axis simulation |
| `Tcd.App/Styles/Generic.xaml` | Master style dictionary merger |

---

## Coding Conventions

- **Indentation**: 2 spaces (per project rules in `.cursor/rules/`)
- **Line length**: ≤ 80 characters preferred
- **Naming**: C# standard (PascalCase for types/methods, camelCase for locals, `_camelCase` for private fields)
- **Async**: All device operations are `async Task`; use `ConfigureAwait(false)` in non-UI code
- **Cancellation**: Use `_activeCts?.Cancel(); _activeCts?.Dispose()` pattern — cancel previous before starting new
- **Thread safety**: UI updates via `App.Current.Dispatcher.Invoke()`; `volatile bool` for cross-thread flags in SimAxis
- **Interlocks**: Throw `InvalidOperationException` with alarm code string; `DelegateSequence` converts to `Alarm` + `Fail` result
- **Testing**: New behavior should have unit tests. Sequence engine tests do not need real device I/O — use `TcdSimulation` for in-process verification
- **Git**: Use feature branches; `main` must always build. Commit messages should focus on *why*, not *what*

### Constants Convention (NO hardcoded ID strings)

모든 ID/키/표시명은 `Tcd.App/Define/` 하위 상수 클래스에 선언하고 참조한다.
리터럴 문자열을 코드에 직접 쓰는 것은 금지.

```
Tcd.App/Define/
  Robot/
    RobotDefine.cs    ← RobotPositionName (UI 표시명), RobotVelocity (기본 속도 %)
  Alarm/
    AlarmKeys.cs      ← AlarmKeys (알람 코드)
  (확장 시)
  Motor/              ← MotorDefine (축 관련 상수, 현재는 Core/Define.cs의 AxisDefine 사용)
  Plc/                ← PlcDefine (I/O 비트/워드 키)
```

- 새 디바이스 타입이 추가되면 해당 디바이스명 폴더를 만들고 `{Device}Define.cs` 파일에 선언
- 네임스페이스: `namespace Tcd.App.Define;` (폴더 구조와 무관하게 단일 네임스페이스)
- 시퀀스 키: `Tcd.Simulator/TcdSequenceKeys.cs` — 이미 상수화됨, 새 시퀀스도 여기 추가

---

## Spec Documents (`../docs/`)

| File | Topic |
|------|-------|
| `CLASS_DESIGN.md` | Full class design: all namespaces, interfaces, and data flows |
| `PRD_Lamination_Simulator.md` | Product requirements, scope, non-functional requirements |
| `ROADMAP.md` | Implementation status and next priorities |
| `SPEC_Sequence.md` | Sequence hierarchy, AUTO flow steps, atomic sequence list |
| `SPEC_UI_Structure.md` | View hierarchy, navigation, layout diagrams, recipe save/load |
| `SPEC_Layout_Recipe.md` | Equipment layout coordinates, teaching positions |
| `SPEC_Motion_SpiiPlus.md` | ACS SPiiPlus variable contract, command flow |
| `SPEC_Robot_Interface.md` | Robot TCP protocol, IRobotDevice, message frames |
| `SPEC_Control_IO.md` | PLC Bit/Word map |
| `SPEC_Interlocks.md` | Interlock rules (implemented + planned) |
| `SPEC_Logging_Csv.md` | CSV log format, 14-day retention |
| `TEST_COVERAGE.md` | Unit test list, fake infrastructure, coverage gaps |
