# SPEC: PLC I/O 맵 (확장 계획)

> 현재 미구현. PLC TCP 시뮬레이터 추가 시 이 맵을 기준으로 구현.

---

## 원칙

- **Source of truth**: PLC 시뮬레이터의 Bit/Word 맵이 모든 설비 상태의 권위
- **WPF**: 맵을 50~200ms 주기로 읽어 HMI에 반영, 시퀀스에 따라 DO/AO 워드 기록
- **통신**: TCP 소켓, 고정 길이 스냅샷, Little-endian

---

## Bit 맵 — PLC → WPF (DI / 상태)

| 주소 | 심볼 | 의미 |
|------|------|------|
| B0.0 | `DI_EStop_OK` | 비상정지 해제 (정상=1) |
| B0.1 | `DI_DoorClosed` | 도어 인터락 닫힘 |
| B0.4 | `DI_LowStageVacSensor` | 하부 스테이지 진공 도달 |
| B0.5 | `DI_HighStageVacSensor` | 상부 스테이지 진공 도달 |
| B0.6 | `DI_RobotGripVacSensor` | 로봇 그립 진공 도달 |
| B0.7 | `DI_UpperChamberVacSensor` | 상부 챔버 자재 고정 진공 도달 |
| B1.2 | `DI_LowerChamberAtReady` | 하부 챔버 레디 위치 |
| B1.3 | `DI_UpperChamberAtReady` | 상부 챔버 레디 위치 |
| B1.4 | `DI_LowerChamberAtBond` | 하부 챔버 합착 위치 |
| B1.5 | `DI_UpperChamberAtBond` | 상부 챔버 합착 위치 |
| B1.6 | `DI_Material_LowStage` | 하부 스테이지 자재 감지 |
| B1.7 | `DI_Material_HighStage` | 상부 스테이지 자재 감지 |
| B2.0 | `DI_AtAtmospheric` | 챔버 대기압 근처 (벤트 후) |

## Bit 맵 — WPF → PLC (DO / 명령)

| 주소 | 심볼 | 의미 |
|------|------|------|
| B3.0 | `DO_VacPumpRequest` | 진공펌프 On 요청 |
| B3.1 | `DO_LowStageVacOn` | 하부 스테이지 진공 On |
| B3.2 | `DO_HighStageVacOn` | 상부 스테이지 진공 On |
| B3.3 | `DO_RobotGripVacOn` | 로봇 그립 진공 On |
| B3.4 | `DO_UpperChamberVacOn` | 상부 챔버 진공 On |
| B3.5 | `DO_ESC_Enable` | ESC 활성 |
| B4.2 | `DO_ChamberMoveToBond` | 챔버 합착 위치 이동 |
| B4.3 | `DO_ChamberMoveToReady` | 챔버 레디 복귀 |
| B4.4 | `DO_LaminationCycleActive` | 2초 합착 구간 |
| B5.2 | `DO_VentValveOpen` | 벤트 밸브 개방 |

## 핸드셰이크

| 주소 | 심볼 | 소유 | 의미 |
|------|------|------|------|
| B6.0 | `Cmd_Start` | 운전원 | 자동 시작 |
| B6.1 | `Cmd_Stop` | 운전원 | 정지 |
| B6.2 | `Sts_SeqRunning` | WPF | 시퀀스 실행 중 |
| B6.3 | `Sts_SeqComplete` | WPF | 사이클 완료 (펄스) |
| B6.4 | `Sts_SeqFault` | WPF | Fault |
| B6.6 | `Alm_Any` | PLC | 알람 래치 |

## Word 맵 — PLC → WPF

| 워드 | 심볼 | 스케일 | 의미 |
|------|------|--------|------|
| W0 | `AI_ChamberPressure_kPa_x100` | kPa×100 | 챔버 압력 |
| W2 | `AI_Loadcell_N_x10` | N×10 | 로드셀 |
| W6 | `RobotPos_X_mm` | mm | 로봇 X |
| W7 | `RobotPos_Y_mm` | mm | 로봇 Y |
| W8 | `RobotPos_Theta_mdeg` | mdeg | 로봇 θ |

## Word 맵 — WPF → PLC

| 워드 | 심볼 | 스케일 | 의미 |
|------|------|--------|------|
| W20 | `AO_ESC_VoltageSet_x100` | V×100 | ESC 설정 전압 |
| W22 | `SeqStepId` | — | 현재 시퀀스 스텝 |
| W23~W25 | `UVW_Corr_*` | μm/mdeg | UVW 가상 보정값 |
