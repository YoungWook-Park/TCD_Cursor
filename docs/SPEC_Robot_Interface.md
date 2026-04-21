# SPEC: 로봇 인터페이스 설계서

| 항목 | 내용 |
|------|------|
| 문서 버전 | 1.0 |
| 작성일 | 2026-04-21 |
| 관련 파일 | `Tcd.Engine/Devices/RobotDeviceContracts.cs`, `Tcd.App/Devices/RobotTcpClient.cs`, `Tcd.Robot.Simulator/` |

---

## 1. 개요

HMI(Tcd.App)와 로봇 컨트롤러(또는 시뮬레이터) 사이의 통신을 정의한다.
인터페이스 계약(`IRobotDevice`)을 통해 실 하드웨어와 시뮬레이터가 동일한 방식으로 교체된다.

### 1.1 통신 방식

| 항목 | 내용 |
|------|------|
| 프로토콜 | TCP/IP |
| 인코딩 | UTF-8 |
| 프레임 구분 | `\n` (개행, Line-delimited JSON) |
| 기본 포트 | 8765 |
| 상태 Push 주기 | 300 ms (Heartbeat) |

### 1.2 연결 구성

```
[Tcd.App]                          [Tcd.Robot.Simulator / 실 로봇 컨트롤러]
   │                                          │
   │  RobotTcpClient (IRobotDevice)           │  SimulatorServer
   │  ─── ConnectAsync(host, port) ──────────►│  TcpListener
   │                                          │    └── ClientSession (연결당 1개)
   │  ─── JSON Command ──────────────────────►│         └── RobotSimCore
   │  ◄── JSON Response / Push ───────────────│
   │  ◄── Heartbeat (300ms State Push) ───────│
```

---

## 2. 도메인 인터페이스 (IRobotDevice)

**위치**: `Tcd.Engine/Devices/RobotDeviceContracts.cs`

```csharp
public interface IRobotDevice
{
    // 연결 상태
    bool IsConnected { get; }

    // 로봇 운전 상태 (ReadLoop가 실시간 갱신)
    bool IsRunning          { get; }
    bool IsHome             { get; }
    bool IsError            { get; }
    RobotPosition CurrentPosition { get; }
    string ErrorMessage     { get; }

    // 상태 변화 이벤트 (백그라운드 스레드에서 발생 → UI에서 Dispatcher 필요)
    event EventHandler<RobotDeviceStateArgs> StateChanged;

    // 연결 관리
    Task ConnectAsync(string host, int port, CancellationToken ct = default);
    void Disconnect();

    // 커맨드
    Task<bool> SetVelocityAsync(RobotPosition position, int pct, CancellationToken ct = default);
    Task<bool> MoveAsync(RobotPosition position, CancellationToken ct = default);
    Task<bool> StopAsync(CancellationToken ct = default);
    Task WaitForPositionAsync(RobotPosition position, TimeSpan timeout, CancellationToken ct);
}
```

### 2.1 구현체 교체 정책

| 모드 | 구현체 | 교체 위치 |
|------|--------|-----------|
| 시뮬레이션 | `RobotTcpClient` → `Tcd.Robot.Simulator` 연결 | `MainCore.cs` |
| 실 하드웨어 | `RobotTcpClient` → 실 로봇 컨트롤러 IP 연결 | `AppSettings.RobotSimHost` 변경 |

`IRobotDevice` 계약이 동일하므로 HMI 코드 변경 없이 교체된다.

---

## 3. 포지션 정의 (RobotPosition)

**위치**: `Tcd.Engine/Devices/RobotDeviceContracts.cs`

| ID | 이름 | 기본 속도 | 설명 |
|----|------|-----------|------|
| 0 | `Home` | 30% | 기본 안전 위치 |
| 10 | `Ready` | 50% | 작업 대기 위치 |
| 11 | `S1_PickupWait` | 60% | S1 스테이지 픽업 대기 |
| 12 | `S1_Pick` | 30% | S1 스테이지 픽업 (CGO 상부 필름) |
| 13 | `S2_PickupWait` | 60% | S2 스테이지 픽업 대기 |
| 14 | `S2_Pick` | 30% | S2 스테이지 픽업 (OCA 하부 필름) |
| 15 | `UpperChamber_PickupWait` | 60% | 상부 챔버 진입 대기 |
| 16 | `UpperChamber_Pick` | 30% | 상부 챔버 소재 안착/픽업 |
| 17 | `LowerChamber_PickupWait` | 60% | 하부 챔버 진입 대기 |
| 18 | `LowerChamber_Pick` | 30% | 하부 챔버 소재 안착/픽업 |
| 19 | `Peel` | 20% | 박리 위치 |

기본 속도는 `Tcd.App/Define/Robot/RobotDefine.cs`의 `RobotVelocity` 상수에 선언.
UI 표시명은 `RobotPositionName` 상수에 선언.

---

## 4. 프로토콜 명세

### 4.1 메시지 타입 상수

**위치**: `Tcd.Robot.Simulator/Protocol/RobotMessages.cs`

| 방향 | T 값 | 설명 |
|------|------|------|
| HMI → Server | `Move` | 지정 포지션으로 이동 시작 |
| HMI → Server | `Stop` | 즉시 정지 |
| HMI → Server | `SetVelocity` | 포지션별 이동 속도 설정 |
| HMI → Server | `SetTeach` | 포지션 좌표(X/Y/Theta) 변경 |
| HMI → Server | `GetState` | 즉시 상태 요청 |
| Server → HMI | `Ack` | 커맨드 수신 응답 |
| Server → HMI | `State` | 전체 상태 스냅샷 (heartbeat / 상태변화) |
| Server → HMI | `Arrived` | 이동 완료 즉시 알림 |

### 4.2 Request 프레임 (HMI → Server)

```json
// Move
{"T":"Move","Pos":11}

// SetVelocity (Move 전 반드시 선행)
{"T":"SetVelocity","Pos":11,"Pct":60}

// Stop
{"T":"Stop"}

// SetTeach (티칭 좌표 업데이트)
{"T":"SetTeach","Pos":11,"X":1600.0,"Y":1300.0,"Theta":-90.0}

// GetState (즉시 상태 요청)
{"T":"GetState"}
```

### 4.3 Response 프레임 (Server → HMI)

```json
// Ack (커맨드 수신 즉시)
{"T":"Ack","Cmd":"Move","Ok":true}
{"T":"Ack","Cmd":"Move","Ok":false,"Err":"Already running."}

// State (300ms heartbeat + 상태변화 시 Push)
{
  "T":"State",
  "Connected":true,
  "Running":false,
  "Home":false,
  "Error":false,
  "Pos":11
}

// Arrived (이동 완료 순간 즉시 Push)
{
  "T":"Arrived",
  "Connected":true,
  "Running":false,
  "Home":false,
  "Error":false,
  "Pos":11
}
```

### 4.4 프레임 처리 규칙

- 모든 프레임은 한 줄 JSON + `\n`으로 구성
- 서버는 커맨드 수신 즉시 `Ack` 반환 후 이동을 비동기 Task로 실행
- `Ack.Ok=false` 시 이동이 시작되지 않은 것 (인터락 거부)
- `State`/`Arrived` 수신 후 클라이언트 로컬 상태 갱신 → `StateChanged` 이벤트

---

## 5. 클라이언트 통신 흐름

### 5.1 연결 수립

```
HMI                               Server
 │── ConnectAsync(host, port) ──►  │
 │                                 │  TcpListener.AcceptTcpClientAsync
 │◄── (TCP 연결 완료) ─────────── │
 │                                 │  ClientSession 생성
 │◄── State Push (즉시) ─────────  │  현재 상태 전송
 │                                 │
 │── ReadLoopAsync 시작 (bg) ──── │
```

### 5.2 이동 명령 시퀀스

```
HMI                                   Server (RobotSimCore)
 │                                         │
 │──{"T":"SetVelocity","Pos":11,"Pct":60}─►│  tp.VelocityPct = 60
 │◄──{"T":"Ack","Cmd":"SetVelocity","Ok":true}─│
 │                                         │
 │──{"T":"Move","Pos":11}────────────────►│  인터락 체크
 │◄──{"T":"Ack","Cmd":"Move","Ok":true}───│  Task.Run(RunMoveAsync) 시작
 │                                         │
 │  [이동 중 300ms마다 State Push]          │
 │◄──{"T":"State","Running":true,...}──────│
 │                                         │
 │◄──{"T":"Arrived","Pos":11,...}──────────│  이동 완료
 │◄──{"T":"State","Running":false,...}─────│  상태 갱신
 │                                         │
 │  WaitForPositionAsync 완료 (폴링 50ms)  │
```

### 5.3 오류 처리

| 상황 | 서버 응답 | 클라이언트 처리 |
|------|-----------|-----------------|
| 인터락 거부 | `Ack.Ok=false`, `Err` 메시지 포함 | `MoveAsync` 반환값 `false`, `LogStatus` 갱신 |
| 이동 중 에러 | `State.Error=true`, `ErrMsg` 포함 | `StateChanged` 이벤트, `LogStatus` 에러 표시 |
| 연결 끊김 | ReadLoop 종료 | `_connected=false`, `StateChanged` 발행 |
| 타임아웃 | 없음 (서버 무응답) | `WaitForPositionAsync` `TimeoutException` 발생 |

---

## 6. 서버 내부 구조

### 6.1 컴포넌트

```
SimulatorServer
  ├── TcpListener         — 포트 바인딩, 연결 수락
  ├── List<ClientSession> — 연결된 클라이언트 목록
  ├── HeartbeatLoop       — 300ms 주기 State 브로드캐스트
  └── RobotSimCore        — 상태 머신 (단일 인스턴스, 공유)
        ├── TeachPoints    — 포지션별 티칭 좌표·속도 저장
        ├── HandleRequest  — 커맨드 처리 (Move/Stop/SetVelocity/SetTeach)
        └── RunMoveAsync   — 거리·속도 기반 이동 시뮬레이션
```

### 6.2 이동 시뮬레이션 계산

```
dist  = √((to.X - from.X)² + (to.Y - from.Y)²)
speed = MaxSpeedMmPerSec × VelocityPct / 100.0
delay = max(dist / speed × 1000, MinMoveMs)
```

- `MaxSpeedMmPerSec` = 800 mm/s (100% 기준)
- `MinMoveMs` = 80 ms (거리 0 포함 최소 시뮬 시간)

### 6.3 인터락 규칙

이동 명령 수신 시 아래 조건을 순서대로 체크. 하나라도 위반 시 `Ack.Ok=false` 반환.

| # | 조건 | 에러 메시지 |
|---|------|-------------|
| 1 | `_isError == false` | `"Error state. Stop and clear first."` |
| 2 | `_isRunning == false` | `"Already running."` |
| 3 | 목적지 포지션 존재 | `"Unknown position {Pos}"` |
| 4 | 현재 위치가 Safe(Home=0 / Ready=10) **또는** 목적지가 Safe | `"Must be at Home(0) or Ready(10) before moving."` |

> **규칙 4 해설**: 어느 작업 포지션(11~19)에 있을 때는 반드시 Ready/Home 경유 후 다른 작업 포지션으로 이동해야 한다.

---

## 7. 멀티 클라이언트 지원

- `SimulatorServer`는 복수 HMI 클라이언트를 동시 수용 (개발·모니터링 목적)
- `RobotSimCore`는 단일 인스턴스 — 모든 클라이언트가 동일한 상태 공유
- 상태 변화(`StatePushed` 이벤트) → `Broadcast()` → 전체 클라이언트에 동시 전송
- 스레드 안전: `_lock` 오브젝트로 모든 상태 필드 보호

---

## 8. 스레드 안전 정책

| 컴포넌트 | 전략 |
|----------|------|
| `RobotSimCore._lock` | 상태 필드 전체를 단일 `object` lock으로 보호 |
| `RobotTcpClient._stateLock` | 상태 캐시 읽기/쓰기 보호 |
| `RobotTcpClient._writeLock` | `StreamWriter.WriteLine` 직렬화 |
| `ClientSession._writeLock` | 서버 측 `StreamWriter.WriteLine` 직렬화 |
| `SimulatorServer._sessLock` | `_sessions` 리스트 추가/제거 보호 |
| UI 바인딩 | `StateChanged` 수신 후 `Dispatcher.Invoke()` 필수 |

---

## 9. 상수 위치 참조

| 상수 종류 | 파일 |
|-----------|------|
| 포지션 UI 표시명 | `Tcd.App/Define/Robot/RobotDefine.cs` → `RobotPositionName` |
| 포지션 기본 속도 | `Tcd.App/Define/Robot/RobotDefine.cs` → `RobotVelocity` |
| 알람 키 | `Tcd.App/Define/Alarm/AlarmKeys.cs` → `AlarmKeys` |
| 메시지 타입 | `Tcd.Robot.Simulator/Protocol/RobotMessages.cs` → `MsgType` |
| 포지션 enum | `Tcd.Engine/Devices/RobotDeviceContracts.cs` → `RobotPosition` |

---

## 10. 향후 확장 고려사항

| 항목 | 현재 | 확장 방향 |
|------|------|-----------|
| Ack 추적 | 미구현 (fire-and-forget에 가까움) | 요청-응답 ID 매핑 (`ReqId` 필드 추가) |
| 실 로봇 연동 | `RobotTcpClient`가 동일 인터페이스로 실 컨트롤러 연결 가능 | 실 프로토콜 서버사이드 어댑터 구현 |
| 티칭 영속성 | 런타임 메모리만 유지 | `SetTeach` 호출 결과를 JSON 파일로 저장 |
| 에러 리셋 | 미구현 | `ClearError` 커맨드 추가 (`MsgType.ClearError`) |
| 그리퍼 제어 | 미구현 | `Grip`/`Release` 커맨드 추가 |
