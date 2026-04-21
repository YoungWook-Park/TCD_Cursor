# SPEC: 시퀀스 상태 머신

| 항목 | 내용 |
|------|------|
| 관련 PRD | [PRD_Lamination_Simulator.md](PRD_Lamination_Simulator.md) |
| I/O | [SPEC_Control_IO.md](SPEC_Control_IO.md) |

---

## 1. 전제

- 초기: **Low / High 스테이지**에 필름 로드, **스테이지 진공 On**으로 고정.
- **상부 자재: CGO**, **하부 자재: OCA**.
- 로봇 이동은 **항상 Ready 위치를 경유**(안전).
- **박리**: 그립퍼 **Grip/Ungrip** (`DO_GripperGrip` / `DO_GripperUngrip`); 레시피 **`SkipPeel`** 이면 박리 관련 스텝 생략.
- **인터록**(도어, E-Stop, 진공 미달 등): 구현 시 **하나씩 추가**; 본 문서는 골격만 정의.

---

## 2. 상태 ID 목록 (WPF 시퀀스 엔진)

| ID | 상태 | 설명 |
|----|------|------|
| 0 | `Idle` | 초기화·대기 |
| 10 | `WaitStart` | `Cmd_Start` 대기 |
| 20 | `RobotPickLow_OCA_VacOn` | 하부(OCA) Pick: 로봇 그립 진공 On 요청 |
| 30 | `RobotPickLow_Handoff` | `DI_RobotGripVacSensor` 후 하부 스테이지 진공 Off |
| 40 | `RobotToReady_AfterLowPick` | Ready 이동(시뮬 지연; PLC가 `RobotPos_*` 갱신) |
| 50 | `RobotToUpperChamber` | 상부 챔버 안착 경로(Ready 경유 완료 후) |
| 60 | `UpperChamber_Grip` | 상부 진공 On, ESC On·전압 SP, 로봇 그립 진공 Off |
| 70 | `RobotReturnReady_AfterUpperPlace` | Ready 복귀 |
| 80 | `PeelOCA` | 박리(`DO_PeelStart`, Grip/Ungrip 시퀀스); `SkipPeel`이면 점프 |
| 90 | `RobotAtReady_BeforeUVW` | Ready 확인 |
| 100 | `UVW_Correction` | 가상 보정값 적용·보간(시뮬) |
| 110 | `ChamberMoveToBond` | 상·하 합착 위치 이동 |
| 120 | `VacPumpAndEvacuate` | `DO_VacPumpRequest`, kPa 목표(시뮬) |
| 130 | `Lamination_2s` | 2.0 s 합착; `DO_LaminationCycleActive`; 샘플링·DB 기록 |
| 140 | `ChamberMoveToReady` | 레디 복귀 |
| 150 | `RobotPickBonded_FromLower` | 합착체 하부에서 Pick(진공 시퀀스 동일 패턴) |
| 160 | `RobotToReady_AfterBondPick` | Ready |
| 170 | `RobotPlaceLow_Stage` | 하부 스테이지 배치·핸드오프 |
| 180 | `Complete` | `Sts_SeqComplete` 펄스, 사이클 종료 |
| 900 | `Fault` | `Sts_SeqFault`, `FaultCode` |
| 910 | `Held` | `Cmd_Stop`/인터록, `Sts_SeqHeld` |

`SeqStepId`(워드 W22)에 현재 ID 기록.

---

## 3. 주요 전이 조건(요약)

| From | To | 조건(개략) |
|------|-----|------------|
| Idle | WaitStart | 인터록 OK(추가 예정), 설비 준비 |
| WaitStart | RobotPickLow_OCA_VacOn | `Cmd_Start` |
| RobotPickLow_OCA_VacOn | RobotPickLow_Handoff | `DO_RobotGripVacOn` 반영 + `DI_RobotGripVacSensor` |
| RobotPickLow_Handoff | RobotToReady_AfterLowPick | `DO_LowStageVacOn`=0 후 타이머/위치 |
| … | … | 로봇 스텝: PLC `RobotPos_*` 목표 도달 또는 시뮬 타이머 |
| UpperChamber_Grip | RobotReturnReady_AfterUpperPlace | `DI_UpperChamberVacSensor`, ESC 피드백 만족 후 로봇 진공 Off |
| RobotAtReady_BeforeUVW | PeelOCA 또는 UVW_Correction | 레시피 `SkipPeel` false/true |
| PeelOCA | RobotAtReady_BeforeUVW | 박리 완료(타이머+Grip/Ungrip 시퀀스) |
| UVW_Correction | ChamberMoveToBond | 보정 완료 |
| ChamberMoveToBond | VacPumpAndEvacuate | `DI_*AtBond` 또는 타이머 |
| VacPumpAndEvacuate | Lamination_2s | `AI_ChamberPressure` 목표 범위(시뮬) |
| Lamination_2s | ChamberMoveToReady | 2.0 s 경과 |
| RobotPlaceLow_Stage | Complete | 하부 스테이지 진공 핸드오프 완료 |
| * | Fault | 진공/위치 타임아웃, `Alm_Any`, 내부 오류 |
| * | Held | `Cmd_Stop` |

**리셋**: `DO_SequenceReset` + `Ack_FaultClear` 절차로 `Idle` 복귀(구현 세부).

---

## 4. 타임아웃(예시, 튜닝)

| 구간 | 타임아웃 | 실패 시 |
|------|----------|---------|
| 진공 도달 | 5~15 s | Fault, `Alm_Any` |
| 로봇 이동 | 3~10 s | Fault |
| 챔버 이동 | 10~30 s | Fault |
| 합착 | 2.5 s 상한 | Fault(이상 시) |

---

## 5. 그립퍼(박리) 시퀀스(예시)

`PeelOCA` 내부는 구현 시 세분화 가능. 예:

1. `DO_GripperGrip` 펄스/유지 → 대기.
2. 박리 모션(로봇 θ 또는 Y 미세 이동 — 레시피 티칭).
3. `DO_GripperUngrip` → 완료.

`SkipPeel=true` 이면 상태 **80** 을 건너뛴다.

---

## 6. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안: CGO/OCA, Grip/Ungrip, SkipPeel, PLC RobotPos 진실 |
