# SPEC: SPiiPlus 연동·버퍼·JobQueue

| 항목 | 내용 |
|------|------|
| 관련 | 장비 디바이스 화면 Timeout 전달, Teach·시퀀스 모션 명령 |

**에이전트 작업 시**: 상위 앱·시퀀스는 **`IMotionCommands`**(도메인 API)만 사용한다. `IMotionGateway`는 ACS 변수 인덱스·`WriteVariable`·`Halt` 등 **통신 원시**이며 퍼사드 구현 내부에서만 다룬다.

---

## 1. 변수 계약 (요약)

| 방향 | 그룹 | 용도 |
|------|------|------|
| PC → ACS | `PC_ACS_DISTANCE(i)`, `PC_ACS_VELOCITY(i)`, `PC_ACS_ACC(i)`, `PC_ACS_DEC(i)`, `PC_ACS_JERK(i)` | 프로파일 (전역 REAL 배열, 인덱스 = 축/버퍼 번호) |
| PC → ACS | `RD_Ena_CMD`, `RD_Disable_CMD`, `RD_Halt_CMD`, `RD_Fcle_CMD`, `RD_Abs_CMD`, `RD_Inc_CMD`, `RD_pJog_CMD`, `RD_nJog_CMD`, `RD_Home_CMD` | 축별 INT 배열; 버퍼 오토루틴 `ON …` 트리거 |
| ACS → PC | `ACS_PC_CURRENT_POS_AXISn`, `ACS_PC_IS_HOME_AXISn`, … `P_LIMIT`/`N_LIMIT` | 모니터링 (버퍼9 등) |
| 공유 | `ON_MONITORING_FLAG` | 버퍼9 루프 제어 |
| 공유 | `HomeMethod(i)`, `HomeFlag(i)` 등 | 홈 방식·완료 플래그 (장비 정의) |

---

## 2. 축 ↔ 버퍼 인덱스 vs 전역 배열 인덱스

- **오토프로그램 / `START (iAXIS), …`**: 축 번호 `i`와 **프로그램 버퍼** 번호가 동일하게 매핑되는 것이 일반적이다.
- **PC→ACS 전역 `REAL(64)` / `INT(64)` (`PC_ACS_*`, `RD_*_CMD`)**: **글로벌 메모리**이므로 `WriteVariable` 시 **`ProgramBuffer.ACSC_NONE`** 을 쓴다. **축 번호는 배열 첨자**로만 쓰이며, 전역 변수 Write에는 프로그램 버퍼 번호를 넣지 않는다.
- 장비마다 예외가 있으면 `EqpDefine` / 기계 상수에만 기록하고, 코드에서는 축 선택에 **`axisBufferIndex`**(= 배열 인덱스) 한 가지로 통일한다.

---

## 3. 버퍼 오토루틴 동작 (계약)

- 각 축 버퍼에 **Servo On/Off**, **Homing**, **Halt**, **Fault clear**, **Abs / Inc / ±Jog** 등이 `ON RD_*_CMD(iAXIS) = 1 …` 형태로 정의된다.
- **선행 Write (PC → ACS)**  
  - **절대 이동 (`RD_Abs_CMD`)**: `PC_ACS_DISTANCE`, `PC_ACS_VELOCITY`, `PC_ACS_ACC`, `PC_ACS_DEC`, `PC_ACS_JERK` 다섯 값을 먼저 쓴 뒤 명령 비트를 올린다.  
  - **증분 이동 (`RD_Inc_CMD`)**: `PC_ACS_DISTANCE`, `PC_ACS_VELOCITY` 두 값을 먼저 쓴 뒤 명령.  
  - **+Jog / −Jog**: `PC_ACS_VELOCITY`를 먼저 쓰고(버퍼에서 부호·가속 조건 처리), **조그 명령은 레벨 제어**한다 (아래 §3.1).  
  - **홈·서보·FClear 등**: 버퍼 정의에 따라 선행 변수 없이 명령만 올리는 경우가 많다.
- **통신 순서**: 한 스캔에 변수가 다 안 보일 수 있으므로, 필요 시 **파라미터 Write 완료 후** 명령 Write(또는 한 틱 지연)를 고려한다.

### 3.1 조그 (hold-to-jog)

- 상위(UI)는 **눌렀을 때** 해당 축의 `RD_pJog_CMD(i)=1` 또는 `RD_nJog_CMD(i)=1`, **뗐을 때** 동일 비트를 **0**으로 쓴다.
- 버퍼 쪽 `ON` 블록은 **조그 비트를 잘못 클리어하면** `TILL … = 0`과 레이스가 나므로, 장비 버퍼는 **상승 에지에서 서브루틴 진입**·**레벨 유지** 규칙과 맞춘다 (현장 버퍼 소스가 단일 기준).
- **비상 정지**: 오토루틴이 비정상일 때 RD 계열만으로 멈추지 않을 수 있어, 상위는 **MMI `Halt` API**로 해당 축을 정지하는 경로를 둔다 (`IMotionCommands.StopAxisAsync` = 조그 비트 클리어 + `Halt`).

### 3.2 `RD_Halt_CMD` vs MMI `Halt`

- 정상 조그 종료는 **조그 CMD 0**.
- **즉시 운동 정지**가 필요하면 **컨트롤러 `Halt`(축 인덱스)** 를 사용한다. `Pulse`로 `RD_Halt_CMD`만 쓰는 방식과 병행할지는 장비 버퍼와 협의.

---

## 4. 버퍼 9 (모니터링)

- 본체: `while(ON_MONITORING_FLAG)` … `FPOS`/`MFLAGS`/`MST`/`FAULT` 매핑.
- **Activate**: `ON_MONITORING_FLAG = 1`, 버퍼 실행(또는 이미 상주 시 플래그만).
- **Deactivate**: `ON_MONITORING_FLAG = 0` → 루프 종료 → (필요 시) STOP → 연결 해제.

---

## 5. PC 측 JobQueue

- **단일 백그라운드 워커**에서 SPiiPlus API 호출(스레드 안전).
- 작업: Connect, Disconnect, 전역 배열 `WriteVariable`(전 벡터), `Halt`, WaitForCondition(폴링+Timeout).
- **Timeout**: 디바이스 설정(ms)을 Connect·각 Job의 `CancellationTokenSource.CancelAfter(timeout)`에 전달.

---

## 6. 코드 매핑 (`Vcd.Motion.SpiiPlus`)

| 개념 | 타입 / 멤버 |
|------|-------------|
| 앱·시퀀스·Teach 공용 API | `IMotionCommands` (`JogPressAsync` / `JogReleaseAsync`, `MoveAbsoluteAsync`, `HomeAsync`, `HaltAxisAsync`, `StopAxisAsync`, …) |
| ACS 원시 | `IMotionGateway.WriteIndexedGlobalRealAsync`, `WriteIndexedGlobalIntAsync`, `PulseIndexedCommandAsync`, `HaltAxisAsync` |
| 변수 이름 문자열 | `MotionVariableNames` |
| PC 전역 미러 | `MotionPcGlobalMirror` + `SpiiPlusVariableAccess` (내부) |

---

## 9. PC 전역 배열 미러 (Write 전 벡터, Activate 시 1회 Read)

- **계약**: `PC_ACS_*`·`RD_*_CMD` 등 PC가 쓰는 **전역 배열**은 ACS 쪽에서 **읽기만** 하고, **쓰기는 PC만** 담당한다.
- **드라이버**: `MotionPcGlobalMirror`에 **길이 64(기본, `MotionOptions.GlobalArrayLength`)** 섀도 배열을 둔다. 한 축 `i`만 바꿔도 해당 **이름의 배열 전체**를 `WriteVariable`(…, `ProgramBuffer.ACSC_NONE`, `-1`×4)로 보낸다.
- **시드 (1회 / 연결)**: 컨트롤러에서 위 전역들을 **한 번 읽어** 미러를 채운다. 트리거는 **`ConnectAsync` 직후** 또는 **`SetMonitoringAsync(true)`** 중 **먼저 도달하는 쪽** 한 번(`SpiiPlusMotionGateway`의 `_mirrorSeeded`). 재연결 시 플래그 리셋 후 다시 시드.
- **`ConnectAsync`**: 이미 연결된 경우 **OpenComm 생략**(no-op). UI는 `IsConnected`일 때 `ConnectAsync`를 호출하지 않는 것을 권장한다.
- **펄스 명령 (`RD_Abs_CMD` 등)**: 미러에서 `i`번을 1로 두고 **전 INT 벡터를 Write**한 뒤, 버퍼가 소비한다고 보고 **미러의 해당 칸만 0으로 되돌린다**(다음 전체 Write에 남은 1이 실리지 않게).
- **Stub**: 동일 미러를 유지하되 버스 Write는 생략하고 시뮬레이터 상태만 갱신한다.

---

## 10. Stub

- ACS DLL 미설치 환경에서는 `StubMotionGateway` + 동일 `MotionCommands`로 UI·시퀀스만 검증.

---

## 11. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안 |
| 2026-04-02 | 버퍼 오토루틴·조그 레벨·선행 Write·`IMotionCommands` 계약 반영 |
| 2026-04-02 | §9 PC 전역 미러·`ACSC_NONE`·Activate 1회 Read·펄스 섀도 클리어 |
