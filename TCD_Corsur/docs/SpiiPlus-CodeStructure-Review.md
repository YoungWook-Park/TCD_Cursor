# SpiiPlus 코드 구조 리뷰

구현 완료 시점 기준, Tcd.App 내 SPII+ 연동 코드의 구조와 역할을 정리한 문서입니다.

---

## 1. 개요

- **위치**: `Tcd.App/Spii/` (및 `Tcd.App/Core/Define.cs` 내 `SpiiDefine`)
- **역할**: ACS SPII+ 제어기와의 통신을 추상화하고, **명령(IMotionService)** 과 **상태 읽기(IAxisStateProvider)** 를 한 곳에서 제공합니다.
- **구성**: 연결 래퍼 1개, 모션 서비스 1개, 상수 정의 1개.

---

## 2. 파일/클래스 구조

| 파일 | 클래스/내용 | 역할 |
|------|-------------|------|
| `Spii/SpiiPlusConnection.cs` | `SpiiPlusConnection` | SPII+ API 래퍼. 변수 읽기/쓰기, 버퍼 실행만 담당. |
| `Spii/SpiiPlusMotionService.cs` | `SpiiPlusMotionService` | `IMotionService` + `IAxisStateProvider` 구현. 명령 실행 + 백그라운드 모니터링 + 캐시 제공. |
| `Core/Define.cs` | `SpiiDefine` | ascpl 변수명·기본 모션 값 상수. `AxisDefine` 은 축 이름·순서. |

---

## 3. SpiiPlusConnection

- **의존성**: `ACS.SPiiPlusNET` (외부 SDK).
- **책임**:
  - 생성 시 `ipAddress` 검증 후 연결 (현재 코드는 `OpenCommSimulator()` 사용).
  - `WriteRealAt(varName, index, value)` / `WriteIntAt(varName, index, value)`: 배열형 변수 한 인덱스에 쓰기.
  - `ReadReal(varName)` / `ReadInt(varName)`: 단일(또는 0번 요소) 읽기.
  - `RunBuffer(bufferNumber)`: 지정 버퍼 실행 (이미 실행 중이면 스킵).
- **특징**: DBUFFER/ascpl 쪽 변수 이름과 인덱스 규칙에 맞춰 사용하는 얇은 래퍼. 재사용·테스트 시 목(mock)으로 대체하기 좋은 경계 계층.

---

## 4. SpiiPlusMotionService

### 4.1 구현 인터페이스

- **IMotionService**: AbsMove, IncMove, Jog, Stop, Home, FaultClear, ServoOn, ServoOff.
- **IAxisStateProvider**: GetAxisState(axisName), GetSnapshot() — 캐시만 반환, 디바이스 직접 호출 없음.
- **IDisposable**: 모니터 태스크 취소 및 연결 정리.

### 4.2 축/버퍼 규칙

- **축 인덱스**: U=0, V=1, W=2, ZLower=3, ZUpper=4 (`AxisDefine.InOrder` 와 동일).
- **ascpl 측**: 0번 버퍼(ABS/STOP 등 명령), 9번 버퍼(모니터링). 생성 시 `ON_MONITORING_FLAG=1` 설정 후 9번 버퍼 실행.

### 4.3 상태 모니터링(캐시) 구조

- **캐시**: `AxisState[] _cache` (길이 5). `_cacheLock` 으로 동기화.
- **백그라운드 태스크**: 생성 시 `MonitorLoopAsync` 를 `Task.Run` 으로 시작. 주기(약 120ms)마다 5축에 대해:
  - `ACS_PC_CURRENT_POS_AXIS{n}`, `ACS_PC_IS_MOVE_AXIS{n}`, `ACS_PC_IS_FAULT_AXIS{n}`, `ACS_PC_IS_HOME_AXIS{n}` 읽기.
  - lock 안에서 `_cache[i]` 갱신.
- **예외**: 루프 내 예외 시 로그 없이 다음 주기로 재시도(연결 끊김 등에 대응).
- **Dispose**: `_monitorCts.Cancel()` → 모니터 종료 대기 후 `_conn.Dispose()`.

### 4.4 명령 흐름 요약

- **AbsMove**: Servo ON → 레시피에서 속도/가감속/저크 로드 → 목표·파라미터 쓰기 → RD_Abs_CMD=1 → isMove/isFault 폴링으로 완료 대기.
- **IncMove**: 캐시에서 현재 위치 읽은 뒤 `current + delta` 로 AbsMove 호출.
- **Jog**: Servo ON → 속도/가감속/저크 쓰기 → RD_pJog_CMD 또는 RD_nJog_CMD=1 → 폴링 루프. 취소/종료 시 finally 에서 Jog 플래그 0으로 정리.
- **Stop**: RD_Halt_CMD(axis)=1.
- **Home**: RD_Ena_CMD, RD_Home_CMD=1 → isHome/isFault 폴링.
- **FaultClear**: RD_Fcle_CMD(axis)=1.
- **ServoOn/Off**: RD_Ena_CMD(axis)=1 또는 0.

### 4.5 레시피/설정 의존

- 속도·가감속·저크는 `MainCore.Instance.Recipes.Current` 에서 읽음. 없으면 `SpiiDefine.Default*` 사용.
- 따라서 레시피 전환이 곧바로 다음 AbsMove/Jog 파라미터에 반영됨.

---

## 5. SpiiDefine (Core/Define.cs)

- **ON_MONITORING_FLAG**, **RD_*_CMD**, **PC_ACS_***: ascpl 변수명과 1:1 매핑.
- **ACS_PC_IS_MOVE_AXIS**, **ACS_PC_IS_FAULT_AXIS**, **ACS_PC_IS_HOME_AXIS**, **ACS_PC_CURRENT_POS_AXIS**: 상태 변수 접두사 (접미사에 축 인덱스 0~4).
- **DefaultVelocity/Acc/Dec/Jerk**: 레시피가 없을 때 사용하는 기본값.

---

## 6. 상위 계층과의 연결

- **MainCore**: `CreateMotionService()` 에서 `UseSpiiPlus == true` 이면 `SpiiPlusMotionService(SpiiIpAddress)` 생성. 동일 인스턴스를 `Motion` 및 `AxisStateProvider` 로 노출.
- **ViewModel**: `Manual_MotorViewModel` 은 `_core.AxisStateProvider` 만 사용. Spii/Sim 분기 및 `SpiiPlusMotionService` 캐스팅 제거됨. 200ms 타이머에서 `GetAxisState` 로 Position/IsMoving/IsFault/IsHome 갱신.

---

## 7. 강점 및 개선 여지

**강점**

- 명령(IMotionService)과 상태(IAxisStateProvider)가 한 서비스에서 제공되어, 실기/시뮬 전환 시 상위에서는 인터페이스만 의존.
- 상태는 백그라운드 모니터가 갱신하고 UI는 캐시만 참조해, UI 스레드에서 디바이스 I/O가 발생하지 않음.
- 변수명·축 인덱스가 `SpiiDefine`/`AxisDefine` 에 모여 있어 ascpl 변경 시 수정 지점이 명확함.

**개선 여지**

- **SpiiPlusConnection** 생성자: 현재 `OpenCommSimulator()` 고정. 실기 Ethernet 연결로 전환 시 `ipAddress` 를 사용하는 API 호출로 교체 필요.
- **모니터 예외 처리**: 연결 끊김/타임아웃 시 로그 또는 알람 연동을 넣으면 디버깅/운영 시 유리함.
- **레시피 의존성**: `MainCore.Instance.Recipes.Current` 에 직접 접근. 테스트·다중 레시피 시나리오 시 생성자/메서드 인자로 레시피 제공자를 주입하는 방식도 고려 가능.

---

## 8. 요약

| 항목 | 내용 |
|------|------|
| 계층 | Connection(통신) → MotionService(명령+상태캐시) → MainCore → ViewModel |
| 상태 소스 | 백그라운드 모니터가 ACS 변수를 주기적으로 읽어 `_cache` 에만 반영. ViewModel 은 캐시만 참조. |
| 축 규칙 | U,V,W,ZLower,ZUpper = 인덱스 0~4. ascpl 변수명은 SpiiDefine, 축 이름은 AxisDefine. |
| 리소스 | SpiiPlusMotionService.Dispose() 에서 모니터 취소 및 Connection 정리. |

이 구조를 기준으로 실기 전환(연결 방식·주소), 로깅/알람, 레시피 주입 방식 등을 단계적으로 확장할 수 있습니다.
