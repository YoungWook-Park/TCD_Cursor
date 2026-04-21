# Implementation Roadmap

Phased order by **dependency** (not calendar). See [TECH_STACK.md](TECH_STACK.md) and [PRD_Lamination_Simulator.md](PRD_Lamination_Simulator.md).

---

## Phase 0 — Baseline

**Goals:** reproducible build and shared settings.

- Confirm `src/Vcd.slnx` builds and WPF starts.
- Centralize paths: recipe root (`D:\VCD_Recipe`), log folder, PLC IP/port (config or constants).
- Optional: agree **Fluent + XAML resource layout** in [DEVELOPMENT_GUIDELINES.md](DEVELOPMENT_GUIDELINES.md) / UI SPEC.

**Refs:** [SPEC_Architecture_Solution.md](SPEC_Architecture_Solution.md), [SPEC_UI_Hmi.md](SPEC_UI_Hmi.md)

---

## Phase 1 — PLC map and simulator

**Goals:** I/O truth in map; wire protocol works.

- Define map **frame** (endian, layout) per [SPEC_Control_IO.md](SPEC_Control_IO.md).
- Implement `Vcd.Plc.Map`: read/write snapshot (in-memory then socket).
- PLC sim (same or separate process): DO to DI/AI (vacuum, kPa, vent, bond bits).
- Unit tests for serialization / round-trip.

**Refs:** [SPEC_Control_IO.md](SPEC_Control_IO.md)

---

## Phase 2 — Snapshot and interlocks

**Goals:** PC-side guards before commands.

- Build **EquipmentSnapshot** from map.
- **InterlockService** + `IInterlockRule`; start with a few rules (pump+bond, atmospheric+motion, upper/lower clash).
- Unit tests with fixed snapshots.

**Refs:** [SPEC_Interlocks.md](SPEC_Interlocks.md)

---

## Phase 3 — Sequence engine (core)

**Goals:** Manual / semi / auto graphs; preflight.

- `SequenceContext`, `StepResult`, `ISequenceStep`, step registry.
- Graph runner: `sequence`, `parallel`, `ref` — [SPEC_Sequence_Engine.md](SPEC_Sequence_Engine.md).
- `Step_PreFlightChecks` (PLC link, Ready, stage vacuum).
- `IModelFlowDescriptor` injection — [SPEC_Model_Flow.md](SPEC_Model_Flow.md).
- Sample JSON under `sequences/` (auto main).

**Refs:** [SPEC_Sequence.md](SPEC_Sequence.md), [SPEC_Sequence_Engine.md](SPEC_Sequence_Engine.md), [SPEC_Model_Flow.md](SPEC_Model_Flow.md)

---

## Phase 4 — Motion (SPiiPlus)

**Goals:** queue + timeout; real or stub.

- `IMotionGateway`, **single-worker JobQueue** — [SPEC_Motion_SpiiPlus.md](SPEC_Motion_SpiiPlus.md).
- Stub for dev without ACS SDK.
- Integrate: connect, `ON_MONITORING_FLAG`, buffer 9, variable pulse API.

**Refs:** [SPEC_Motion_SpiiPlus.md](SPEC_Motion_SpiiPlus.md)

---

## Phase 5 — CSV logging

**Goals:** file log + retention; viewer shell.

- CSV writer `Timestamp,Level,BlockName,Message` — [SPEC_Logging_Csv.md](SPEC_Logging_Csv.md).
- Cleanup: max **14 days** (app start / timer).
- Main window **Log** button opens viewer (file list + read; no live stream on main).

**Refs:** [SPEC_Logging_Csv.md](SPEC_Logging_Csv.md)

---

## Phase 6 — WPF HMI

**Goals:** operator UI and module wiring.

- Fluent dark shell, `WindowStyle=None`, bottom nav (Device / Recipe / Teach / Manual), min/exit.
- **Device:** PLC IP, sim flag, timeouts; motion timeout.
- **Manual:** DO/DI grid, kPa / regulator / ESC, stage vacuum buttons (green/red states).
- **Recipe:** JSON list, apply / copy / delete, save-load, `No Recipe`.
- **Teach:** table, Jog/Inc/**Stop (red)**, recipe save-load on pane, axis status.
- `IDialogService`.
- Bind **Start/Stop** to sequence engine.

**Refs:** [SPEC_UI_Hmi.md](SPEC_UI_Hmi.md), [SPEC_Layout_Recipe.md](SPEC_Layout_Recipe.md)

**HMI/시퀀스 실행 계획·규약:** [PLAN_Hmi_Sequence_Execution.md](PLAN_Hmi_Sequence_Execution.md), [CONVENTIONS_Hmi_Sequences.md](CONVENTIONS_Hmi_Sequences.md)

---

## Phase 7 — History, 3D, analytics

**Goals:** end-to-end portfolio story.

- Lamination **2 s** sampling to **MongoDB** (or interim file store).
- **Helix** 3D (optional) after UI stable.
- **Python + React** dashboard for run/trend views (local).

**Refs:** PRD 6.3, [SPEC_Layout_Recipe.md](SPEC_Layout_Recipe.md)

---

## Change History

| Date | Summary |
|------|---------|
| 2026-04-01 | Link PLAN/CONVENTIONS for HMI·sequence execution and naming. |
| 2026-03-31 | Initial ROADMAP |
