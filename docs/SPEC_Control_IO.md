# SPEC: 제어 I/O 맵 (Bit / Word) 및 통신 원칙

| 항목 | 내용 |
|------|------|
| 관련 PRD | [PRD_Lamination_Simulator.md](PRD_Lamination_Simulator.md) |

---

## 1. 원칙

- **Source of truth**: **PLC 시뮬레이터**가 보유하는 **Bit/Word 맵**이 모든 설비 상태의 진실이다 (자재 감지, 진공 도달, kPa, 알람 등).
- **WPF**: 맵을 **주기적으로 읽어** HMI·3D에 반영하고, 시퀀스에 따라 **DO/ AO 워드**를 쓴다.
- **PLC 시뮬**: DO를 읽어 **가상 동작**(펌프, 챔버 압력 램프, 레귤레이터·로드셀, DI)을 갱신한다.
- **통신**: **TCP 소켓**, **고정 프레이밍** 또는 **고정 길이 스냅샷**; **Little-endian** 권장 (구현 시 명시).
- **주기**: **50~200 ms** 맵 교환 (튜닝).
- **단위**: 압력 **kPa** (워드는 스케일 저장, 예: kPa×100).

---

## 2. Bit 맵 — PLC → WPF (DI / 상태)

| 주소 | 심볼 | 의미 |
|------|------|------|
| B0.0 | `DI_EStop_OK` | 비상정지 해제(정상=1) |
| B0.1 | `DI_DoorClosed` | 도어 인터록 닫힘 |
| B0.2 | `DI_AirSupplyOK` | 공압(CDA) OK |
| B0.3 | `DI_VacPumpRunning` | 진공펌프 Run 피드백 |
| B0.4 | `DI_LowStageVacSensor` | 하부 스테이지 진공 도달 |
| B0.5 | `DI_HighStageVacSensor` | 상부 스테이지 진공 도달 |
| B0.6 | `DI_RobotGripVacSensor` | 로봇 그립(자재) 진공 도달 |
| B0.7 | `DI_UpperChamberVacSensor` | 상부 챔버 자재 고정 진공 도달 |
| B1.0 | `DI_ESC_ActiveFb` | ESC 활성 피드백 |
| B1.1 | `DI_UVW_HomeOK` | UVW 허용/홈 완료 |
| B1.2 | `DI_LowerChamberAtReady` | 하부 챔버 레디 위치 |
| B1.3 | `DI_UpperChamberAtReady` | 상부 챔버 레디 위치 |
| B1.4 | `DI_LowerChamberAtBond` | 하부 챔버 합착 위치 |
| B1.5 | `DI_UpperChamberAtBond` | 상부 챔버 합착 위치 |
| B1.6 | `DI_Material_LowStage` | 하부 스테이지 자재 감지 (PLC가 시뮬 로직으로 유지) |
| B1.7 | `DI_Material_HighStage` | 상부 스테이지 자재 감지 |
| B2.0 | `DI_AtAtmospheric` | 챔버가 대기압 근처(벤트 후 허용). 미사용 시 `AI_ChamberPressure` 임계로만 판정 가능 |
| B2.1 | `DI_VentValveOpenFb` | 벤트 밸브 개방 피드백(선택; 없으면 DO 래치만 사용) |
| B2.2 | `Reserve_DI_B2_2` | 예비 |
| B2.3 | `Reserve_DI_B2_3` | 예비 |

---

## 3. Bit 맵 — WPF → PLC (DO / 명령)

| 주소 | 심볼 | 의미 |
|------|------|------|
| B3.0 | `DO_VacPumpRequest` | 진공펌프 On 요청 |
| B3.1 | `DO_LowStageVacOn` | 하부 스테이지 진공 On |
| B3.2 | `DO_HighStageVacOn` | 상부 스테이지 진공 On |
| B3.3 | `DO_RobotGripVacOn` | 로봇 그립 진공 On |
| B3.4 | `DO_UpperChamberVacOn` | 상부 챔버 진공 On |
| B3.5 | `DO_ESC_Enable` | ESC 활성 |
| B3.6 | `DO_RobotProgramStart` | 로봇 동작 트리거(시뮬) |
| B3.7 | `DO_SequenceReset` | 시퀀스/알람 리셋 요청 |
| B4.0 | `DO_PeelStart` | 박리 시퀀스 시작(시뮬) |
| B4.1 | `DO_UVW_CorrectionStart` | UVW 보정 시작 |
| B4.2 | `DO_ChamberMoveToBond` | 챔버 합착 위치 이동 |
| B4.3 | `DO_ChamberMoveToReady` | 챔버 레디 복귀 |
| B4.4 | `DO_LaminationCycleActive` | 2초 합착 구간 표시(WPF) |
| B4.5 | `DO_SeqAbort` | 비정상 중단 |
| B5.0 | `DO_GripperGrip` | 그립퍼 Grip(박리용) |
| B5.1 | `DO_GripperUngrip` | 그립퍼 Ungrip |
| B5.2 | `DO_VentValveOpen` | 진공 파기용 벤트 밸브 개방(1=개방). 합착 완료 후 대기압 회복에 사용 |
| B5.3 | `Reserve_DO_B5_3` | 예비 |

*Grip/Ungrip은 상호 배타 또는 펄스 규칙을 시퀀스 문서에서 정의.*

### 3.1 진공펌프 On 조건 (PLC 진실, 요약)

- **`DO_VacPumpRequest` 허용(래치)** 은 PLC가 판단: **`DI_UpperChamberAtBond` AND `DI_LowerChamberAtBond`** (및 도어·E-Stop 등 추가 인터록)일 때만 유효.
- PC/WPF는 선검사용으로 동일 규칙을 [SPEC_Interlocks.md](SPEC_Interlocks.md)와 맞춘다.

### 3.2 대기압·벤트 (요약)

- 합착 종료 후: **`DO_VentValveOpen`=1** → 챔버 압력이 대기압 근처(`DI_AtAtmospheric`=1 또는 `AI_ChamberPressure_kPa_x100` ≥ 설정 임계, 게이지 kPa 기준은 구현 시 고정)까지 대기.
- **대기압 미달성 시** 챔버 Z축 등 위험 이동은 PLC·PC 인터록에서 금지. 상세는 [SPEC_Interlocks.md](SPEC_Interlocks.md).

---

## 4. 핸드셰이크·명령 (공유 맵 영역)

| 주소 | 심볼 | 소유(쓰기) | 의미 |
|------|------|------------|------|
| B6.0 | `Cmd_Start` | 운전원/PLC 시뮬 패널 | 자동운전 시작 |
| B6.1 | `Cmd_Stop` | 운전원/PLC 시뮬 | 정지 |
| B6.2 | `Sts_SeqRunning` | WPF | 시퀀스 실행 중 |
| B6.3 | `Sts_SeqComplete` | WPF | 1사이클 정상 완료(펄스 권장) |
| B6.4 | `Sts_SeqFault` | WPF | Fault |
| B6.5 | `Sts_SeqHeld` | WPF | 홀드 |
| B6.6 | `Alm_Any` | PLC | 알람 래치 |
| B6.7 | `Ack_FaultClear` | PLC | 알람 클리어 확인 |

---

## 5. Word 맵 — PLC → WPF

| 워드 | 심볼 | 스케일 | 의미 |
|------|------|--------|------|
| W0 | `AI_ChamberPressure_kPa_x100` | kPa×100 | 챔버 압력 |
| W1 | `AI_RegulatorPressure_kPa_x100` | kPa×100 | 레귤레이터 압력 |
| W2 | `AI_Loadcell_N_x10` | N×10 | 로드셀 |
| W3 | `AI_ESC_Voltage_x100` | V×100 | ESC 전압 피드백 |
| W4 | `SeqCycleId` | — | 사이클 ID |
| W5 | `FaultCode` | — | Fault 코드 (0=정상) |
| W6 | `RobotPos_X_mm` | mm | 로봇 TCP X (PLC 시뮬 위치 진실) |
| W7 | `RobotPos_Y_mm` | mm | 로봇 TCP Y |
| W8 | `RobotPos_Theta_mdeg` | mdeg | 로봇 TCP θ |
| W9..W15 | `Reserve_AI` | — | 예비 |

---

## 6. Word 맵 — WPF → PLC

| 워드 | 심볼 | 스케일 | 의미 |
|------|------|--------|------|
| W20 | `AO_ESC_VoltageSet_x100` | V×100 | ESC 설정 전압 |
| W21 | `AO_RegulatorSetpoint_kPa_x100` | kPa×100 | 레귤레이터 SP(선택) |
| W22 | `SeqStepId` | — | WPF 현재 시퀀스 스텝(모니터) |
| W23 | `UVW_Corr_X_um` | μm | 가상 보정 X |
| W24 | `UVW_Corr_Y_um` | μm | 가상 보정 Y |
| W25 | `UVW_Corr_Theta_mdeg` | mdeg | 가상 보정 θ |
| W26 | `RecipeId` | — | 선택 레시피 ID |
| W27..W31 | `Reserve_AO` | — | 예비 |

---

## 7. 자재 감지 DI (PLC 유지)

실제 설비에서는 센서가 PLC DI로 들어온다. 시뮬에서도 **`DI_Material_LowStage` / `DI_Material_HighStage`** 는 **PLC 시뮬**이 WPF DO·시퀀스 단계·가상 물리 규칙에 따라 갱신한다. WPF는 이를 **읽기만** 하여 인터록·HMI에 사용한다 (진실은 PLC).

---

## 8. 인터락 요약 (교차 참조)

| 항목 | 요지 |
|------|------|
| 펌프 On | 양 챔버 합착 위치 Bit AND |
| 상·하 충돌 | 상부가 합착 한계를 넘으면 하부 이동 금지 등 — 규칙 표는 [SPEC_Interlocks.md](SPEC_Interlocks.md) |
| 모터 이동 | 벤트·대기압·진공 조건 만족 후 허용 |
| 권위 | **PLC 맵** 최종; PC는 `InterlockService`로 선검사 |

---

## 9. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안: Grip/Ungrip, 로봇 위치 워드, PLC SoT 반영 |
| 2026-03-31 | Vent DO, 대기압 DI, 펌프/벤트 인터락 요약 |
