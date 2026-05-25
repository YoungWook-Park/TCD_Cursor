# TCD_Cursor 클래스 설계서

> 작성일: 2026-05-25
> 대상 솔루션: `TCD_Corsur.sln`
> 프로젝트: Thermal Compression Bonding HMI 시뮬레이터

---

## 1. 솔루션 구조 및 의존 관계

```
Tcd.App  (net8.0-windows, WPF EXE)
  ├── Tcd.Engine  (netstandard2.0, 도메인 라이브러리)
  └── Tcd.Simulator  (netstandard2.0, 인-프로세스 시뮬레이터)
       └── Tcd.Engine

Tcd.Robot.Simulator  (net8.0, 독립 TCP 서버 EXE)
  └── Tcd.Engine (간접)
```

의존 방향은 단방향 위계: `App → Simulator → Engine`
`Engine`은 외부 의존성 없는 순수 도메인 라이브러리.

---

## 2. 네임스페이스 맵

| 네임스페이스 | 위치 | 역할 |
|---|---|---|
| `Tcd.Core` | Tcd.Engine | 알람, 시간, 인터페이스 기반 |
| `Tcd.Core.Logging` | Tcd.Engine | 로깅 계층 |
| `Tcd.Sequence` | Tcd.Engine | 시퀀스 엔진 |
| `Tcd.Devices` | Tcd.Engine | 디바이스 계약(인터페이스) |
| `Tcd.Materials` | Tcd.Engine | 자재 모델/트래커 |
| `Tcd.Simulator` | Tcd.Simulator | 인-프로세스 하드웨어 시뮬레이션 |
| `Tcd.App.Core` | Tcd.App | 컴포지션 루트, 레시피, 설정 |
| `Tcd.App.Define` | Tcd.App | 상수 클래스(알람키, 로봇 정의) |
| `Tcd.App.Devices` | Tcd.App | TCP 로봇 클라이언트 구현 |
| `Tcd.App.Spii` | Tcd.App | SPiiPlus 모션 서비스 구현 |
| `Tcd.App.Sequences.*` | Tcd.App | 수동/반자동/자동 시퀀스 |
| `Tcd.App` (루트) | Tcd.App | ViewModel, MVVM 기반 |

---

## 3. Tcd.Engine — 도메인 계층

### 3-1. 알람 (Tcd.Core)

```
IAlarmSink                          <<interface>>
  + Raise(alarm: Alarm): void

AlarmManager                        implements IAlarmSink
  - _alarms: List<Alarm>            (lock _gate)
  + AlarmRaised: EventHandler<Alarm>
  + Snapshot(): IReadOnlyList<Alarm>
  + Raise(alarm: Alarm): void
  + Raise(code, message, severity): void

Alarm                               (sealed, immutable)
  + Code: string
  + Message: string
  + Severity: AlarmSeverity
  + Timestamp: DateTimeOffset

AlarmSeverity                       <<enum>>
  Info | Warning | Error
```

### 3-2. 시간 추상화 (Tcd.Core)

```
ITimeProvider                       <<interface>>
  + Now: DateTimeOffset
  + Delay(duration, ct): Task

SystemTimeProvider                  implements ITimeProvider
  (실시간 DateTimeOffset.Now / Task.Delay)
```

### 3-3. 시퀀스 엔진 (Tcd.Sequence)

```
ISequence                           <<interface>>
  + Key: string
  + DisplayName: string
  + ExecuteAsync(context, parameter, ct): Task<SequenceResult>

DelegateSequence                    implements ISequence
  - _body: Func<ISequenceContext, object, CancellationToken, Task>
  + 생성자(key, displayName, body)
  // OperationCanceledException → Stopped
  // Exception → Alarm 발생 후 Fail

ISequenceContext                    <<interface>>
  + Alarms: IAlarmSink
  + Time: ITimeProvider
  + StopToken: CancellationToken

SequenceManager
  - _sequences: Dictionary<string, ISequence>
  + Trace: EventHandler<SequenceTraceEventArgs>
  + Register(sequence): void
  + Contains(key): bool
  + List(): IReadOnlyCollection<ISequence>
  + RunAsync(key, context, parameter, ct): Task<SequenceResult>

SequenceResult                      (sealed, factory methods)
  + Status: SequenceStatus
  + Error: string
  + Success(): SequenceResult       (static)
  + Fail(error): SequenceResult     (static)
  + Stopped(): SequenceResult       (static)

SequenceStatus                      <<enum>>
  Succeeded | Failed | Stopped

SequenceTraceEventArgs
  + Key, DisplayName, Kind, Status, Error, Timestamp
```

### 3-4. 디바이스 계약 (Tcd.Devices)

```
IMotionService                      <<interface>>
  + AbsMoveAsync(axis, targetPos, ct): Task
  + IncMoveAsync(axis, delta, ct): Task
  + JogAsync(axis, velocity, ct): Task
  + StopAsync(axis, ct): Task
  + HomeAsync(axis, ct): Task
  + FaultClearAsync(axis, ct): Task
  + ServoOnAsync(axis, ct): Task
  + ServoOffAsync(axis, ct): Task
  // axis: 논리명 "U","V","W","ZLower","ZUpper"

IAxisStateProvider                  <<interface>>
  + GetAxisState(axisName): AxisState
  + GetSnapshot(): IReadOnlyList<AxisState>

AxisState                           (mutable snapshot, UI 캐시용)
  + AxisName: string
  + Position: double
  + IsMoving: bool
  + IsFault: bool
  + IsHome: bool
  + IsServoOn: bool
  + IsLimitPos: bool
  + IsLimitNeg: bool

IRobot                              <<interface>>
  + CurrentPosition: RobotPosition
  + HasVacuum: bool
  + CommandMoveToAsync(position, ct): Task
  + WaitForPositionAsync(position, timeout, ct): Task
  + PickAsync(from, ct): Task
  + PlaceAsync(to, ct): Task

IRobotDevice                        <<interface>>   (TCP 클라이언트용)
  + IsConnected: bool
  + IsRunning: bool
  + IsHome: bool
  + IsError: bool
  + CurrentPosition: RobotPosition
  + ErrorMessage: string
  + StateChanged: EventHandler<RobotDeviceStateArgs>
  + ConnectAsync(host, port, ct): Task
  + Disconnect(): void
  + SetVelocityAsync(position, pct, ct): Task<bool>
  + MoveAsync(position, ct): Task<bool>
  + StopAsync(ct): Task<bool>
  + WaitForPositionAsync(position, timeout, ct): Task

RobotPosition                       <<enum>>
  Home=0, Stage=1,
  UpperChamberLoad=2, LowerChamberLoad=3,
  Ready=10, S1_PickupWait=11, S1_Pick=12,
  S2_PickupWait=13, S2_Pick=14,
  UpperChamber_PickupWait=15, UpperChamber_Pick=16,
  LowerChamber_PickupWait=17, LowerChamber_Pick=18,
  Peel=19

IPlc                                <<interface>>
  + WaitForStageLoadedAsync(timeout, ct): Task<bool>
```

### 3-5. 자재 모델 (Tcd.Materials)

```
Material                            (sealed, immutable record-style)
  + Id: Guid
  + Kind: MaterialKind
  + State: MaterialState
  + Location: MaterialLocation
  + With(state?, location?): Material   (새 인스턴스 반환)

MaterialKind                        <<enum>>
  UpperFilm | LowerFilm | BondedProduct

MaterialState                       <<enum>>
  None | Loaded | InProcess | Completed | Scrapped

MaterialLocation                    <<enum>>
  None | Stage1 | Stage2 | Robot | UpperChamber | LowerChamber

IMaterialTracker                    <<interface>>
  + IsOccupied(location): bool
  + Get(location): Material
  + Snapshot(): IReadOnlyDictionary<MaterialLocation, Material>
  + Place(material, location): void
  + Remove(location): Material
  + Move(from, to): void
  + Clear(): void

InMemoryMaterialTracker             implements IMaterialTracker
  - _byLocation: Dictionary<MaterialLocation, Material>   (lock _gate)
  // Place: 이미 점유 시 InvalidOperationException
  // Remove: 없으면 null 반환
  // Move: 원본 없거나 대상 점유 시 예외
```

### 3-6. 로깅 (Tcd.Core.Logging)

```
ILogWriter                          <<interface>>
  + Log(level, ctx, stepName, message, data, ex): void
  + Trace/Debug/Info/Warn/Error/Fatal(...): void

ILogSink                            <<interface>>
  + MinLevel: LogLevel
  + WriteBatchAsync(entries, ct): Task

LogWriter                           implements ILogWriter
  - _queue: BlockingCollection<LogEntry>
  - _sink: ILogSink
  - _consumerTask: Task             (백그라운드 배치 소비)
  + Start(): void
  + Stop(): void
  // 호출자는 Enqueue만, 백그라운드가 ILogSink로 일괄 기록

FileLogSink                         implements ILogSink
  (CSV 파일, 14일 보관)

LogEntry                            (record-style)
  + Timestamp, Level, SequenceKey, RunId,
    StepName, AxisName, Message, Data, ExceptionMessage

LogContext                          (struct-like)
  + SequenceKey: string
  + RunId: Guid
  + AxisName: string

LogLevel                            <<enum>>
  Trace | Debug | Info | Warn | Error | Fatal
```

---

## 4. Tcd.Simulator — 시뮬레이션 계층

### 4-1. 시뮬레이션 루트

```
TcdSimulation                       implements ISequenceContext
  + Alarms: IAlarmSink              (AlarmManager 인스턴스)
  + Time: ITimeProvider             (SystemTimeProvider)
  + StopToken: CancellationToken
  + Materials: IMaterialTracker     (InMemoryMaterialTracker)
  + Robot: IRobot                   (SimRobot)
  + LowerMotion: SimLowerChamberMotion
  + Plc: IPlc                       (SimPlc)
  + BindStopToken(ct): void
  + LoadStage(stage1Kind, stage2Kind): void
  + Reset(): void
```

### 4-2. 축/모션 시뮬레이션

```
SimAxis                             (sealed)
  - _position: double               (lock _gate)
  - _isMoving: volatile bool
  - _unitsPerSecond: double
  + Name: string
  + IsServoOn: bool
  + IsMoving: bool
  + Position: double
  + CommandMoveToAsync(position, ct): Task
  + WaitForInPositionAsync(position, tolerance, timeout, ct): Task
  // 내부적으로 비동기 이동 시뮬레이션 (RunMoveAsync)

SimLowerChamberMotion
  + U: SimAxis
  + V: SimAxis
  + W: SimAxis
  + Z: SimAxis                      (ZLower 논리명)

SimMotionService                    implements IMotionService, IAxisStateProvider
  - _sim: TcdSimulation
  - _settings: AppSettingsProxy
  + GetAxisState(axisName): AxisState
  + GetSnapshot(): IReadOnlyList<AxisState>
  + AbsMoveAsync / IncMoveAsync / JogAsync / StopAsync
  + HomeAsync / FaultClearAsync / ServoOnAsync / ServoOffAsync
  // Axis("U"|"V"|"W"|"ZLower"|"ZUpper") → SimAxis 매핑

AppSettingsProxy                    (설정 값 전달용 경량 DTO)
  + AxisMoveTimeout: TimeSpan
```

### 4-3. 로봇/PLC 시뮬레이션

```
SimRobot                            implements IRobot
  - _time: ITimeProvider
  - _materials: IMaterialTracker
  + CurrentPosition: RobotPosition
  + HasVacuum: bool
  + CommandMoveToAsync(position, ct): Task
  + WaitForPositionAsync(position, timeout, ct): Task
  + PickAsync(from, ct): Task
  + PlaceAsync(to, ct): Task

SimPlc                              implements IPlc
  + WaitForStageLoadedAsync(timeout, ct): Task<bool>
```

### 4-4. 시퀀스 레지스트리

```
TcdSequenceKeys                     (static 상수 클래스)
  // 모든 시퀀스 키 문자열 상수 (Robot_*, AxisU_*, SEMI_*, AUTO_*, ...)
  // 새 시퀀스 추가 시 반드시 이 파일에 상수 선언

TcdSequenceRegistry                 (static factory)
  + Build(sim, motion): SequenceManager
  // 원자 시퀀스(Robot 이동, Axis 이동, Material 생성, Delay) 등록

TcdAutoSequenceFactory              (static)
  // AUTO 시퀀스 빌더
```

---

## 5. Tcd.App — 애플리케이션 계층

### 5-1. 컴포지션 루트 (Tcd.App.Core)

```
MainCore                            (singleton)
  - _lazy: Lazy<MainCore>
  + Instance: MainCore              (static)
  + IsInitialized: bool
  + Settings: AppSettings
  + Recipes: RecipeStore
  + RecipeRepository: IRecipeRepository
  + Alarms: AlarmManager
  + Simulation: TcdSimulation
  + Sequences: SequenceManager
  + Motion: IMotionService
  + AxisStateProvider: IAxisStateProvider
  + Log: ILogWriter
  + LogContext: LogContext
  + RobotDevice: IRobotDevice
  + Initialize(): void
  // UseSpiiPlus=true → SpiiPlusMotionService
  // UseSpiiPlus=false → SimMotionService

AppSettings
  + StageLoadTimeout: TimeSpan      (5s)
  + RobotMoveTimeout: TimeSpan      (2s)
  + AxisMoveTimeout: TimeSpan       (3s)
  + UseSpiiPlus: bool               (false)
  + SpiiIpAddress: string
  + RobotSimHost: string            ("127.0.0.1")
  + RobotSimPort: int               (7001)
```

### 5-2. 레시피 모델 (Tcd.App.Core)

```
TcdModel
  + Name: string
  + Recipes: List<TcdRecipe>

TcdRecipe
  + Version: int
  + ModelName: string
  + Name: string
  + AxisTeach: Dictionary<string, double>
    // 키: "U","V","W","ZLower","ZUpper"
  + RobotTeach: Dictionary<string, RobotPosition>
  + RobotVelocity: Dictionary<string, int>    (1-100%)
  + MotionVelocity / MotionAcc / MotionDec / MotionJerk: double
  + GetAxis(key, fallback): double
  + SetAxis(key, value): void
  + GetRobotVelocity(positionName): int

RecipeStore
  + Models: IReadOnlyList<TcdModel>
  + Items: IReadOnlyList<TcdRecipe>
  + Current: TcdRecipe?
  + CurrentModel: TcdModel?
  + CurrentChanged: EventHandler
  + SetCurrentRecipe(recipe): void
  + LoadOrCreateDefaults(repo): RecipeStore   (static)

IRecipeRepository                   <<interface>>
  + RecipesDirectory: string
  + ListRecipeNames(): IReadOnlyList<string>
  + Load(name): TcdRecipe
  + Save(recipe): void

JsonRecipeRepository                implements IRecipeRepository
  (JSON 파일 직렬화, %AppData% 하위 경로)
```

### 5-3. 상수 정의 (Tcd.App.Define)

```
AlarmKeys                           (static)
  // 알람 코드 문자열 상수

RobotPositionName                   (static)
  Home, Ready, S1_PickupWait, S1_Pick, S2_PickupWait, S2_Pick,
  UpperChamber_PickupWait, UpperChamber_Pick,
  LowerChamber_PickupWait, LowerChamber_Pick, Peel

RobotVelocityDefault                (static)
  // 포지션별 기본 속도 % (int 상수)

AxisDefine                          (Tcd.App.Core.Define — static)
  U="U", V="V", W="W", ZLower="ZLower", ZUpper="ZUpper"
  InOrder: string[]   // 순서 보장 배열
```

### 5-4. 디바이스 구현 (Tcd.App.Devices / Tcd.App.Spii)

```
RobotTcpClient                      implements IRobotDevice
  // TCP 소켓으로 로봇 시뮬레이터 서버와 통신
  // JSON 메시지 프로토콜
  // StateChanged 이벤트: 배경 스레드에서 발생

SpiiPlusMotionService               implements IMotionService, IAxisStateProvider
  // ACS SPiiPlus SDK 래핑
  // 실 하드웨어 연결 시 사용

SpiiPlusConnection
  // SDK 연결 수명 관리
```

### 5-5. 시퀀스 — 수동 (Tcd.App.Sequences.Manual)

각 축별로 8개 시퀀스를 `SequenceManager`에 등록.

```
Manual_AxisU / Manual_AxisV / Manual_AxisW
Manual_AxisZLower / Manual_AxisZUpper        (모두 static factory 클래스)
  + RegisterAll(sequences: SequenceManager): void
  // 등록 시퀀스: AbsMove, IncMove, JogMove, Stop, Home, FaultReset, ServoOn, ServoOff
```

### 5-6. 시퀀스 — 반자동 (Tcd.App.Sequences.SemiAuto)

```
SemiAutoLoadUpperFilmSequence       implements ISequence
  // Stage1 → UpperChamber 자재 이동 (로봇 픽앤플레이스)

SemiAutoLoadLowerFilmSequence       implements ISequence
  // Stage2 → LowerChamber 자재 이동

SemiAutoAlignUVWSequence            implements ISequence
  // U/V/W 축 레시피 포지션으로 이동 (정렬)

SemiAutoBondSequence                implements ISequence
  // ZLower/ZUpper 합착 이동 + 드웰 + 원위치

SemiAutoUnloadProductToStage2Sequence implements ISequence
  // LowerChamber → Stage2 제품 언로드
```

### 5-7. 시퀀스 — 자동 (Tcd.App.Sequences.Auto)

```
AutoRunSequence                     implements ISequence
  // 전체 사이클:
  //   LoadUpperFilm → LoadLowerFilm → AlignUVW → Bond → UnloadProduct
  // 내부에서 SEMI_* 시퀀스를 순차 호출
```

---

## 6. MVVM 기반 (Tcd.App.Mvvm)

```
NotifyPropertyChangedBase           (abstract)
  + Set<T>(ref field, value): bool
  + Raise(propertyName): void
  // INotifyPropertyChanged 구현 공통 기반

RelayCommand                        implements ICommand
  - _execute: Action<object?>
  - _canExecute: Predicate<object?>?
  + Execute(parameter): void
  + CanExecute(parameter): bool

BiRelayCommand                      extends RelayCommand
  + RaiseCanExecuteChanged(): void
  // canExecute 조건이 바뀔 때 외부에서 호출
```

---

## 7. ViewModel 계층

```
MainWindowViewModel                 extends NotifyPropertyChangedBase
  - _core: MainCore
  - _sim: TcdSimulation
  - _seq: SequenceManager
  - _uiTimer: DispatcherTimer       (200ms 폴링)
  - _runCts: CancellationTokenSource?

  // 자식 ViewModel
  + Recipe: RecipeViewModel
  + Manual: ManualViewModel
  + CurrentContent: object          (Main / Recipe / Manual 전환)

  // 상태 프로퍼티
  + IsRunning: bool
  + Status: string
  + Stage1/Stage2/UpperChamber/LowerChamber: string
  + Stage1/2/UpperChamber/LowerChamberHasMaterial: bool
  + ZPosition: double
  + CurrentRobotPosition: RobotPosition
  + IsBonding: bool                 (HMI 합착 애니메이션)
  + RobotHasVacuum: bool
  + Alarms: ObservableCollection<string>
  + CurrentRecipeName: string

  // 커맨드
  + Cmd_LoadStageCommand
  + Cmd_StartAutoCommand
  + Cmd_StopCommand
  + Cmd_UnloadProductCommand
  + Cmd_ClearCommand
  + Cmd_ExitCommand
  + Cmd_ShowMainPage / ShowRecipePage / ShowManualPage

  - RefreshSnapshot(): void         (DispatcherTimer 콜백)
  - RunSequenceAsync(key, param, runningStatus, doneStatus): void

RecipeViewModel                     extends NotifyPropertyChangedBase
  // Motor 탭: 축별 티칭 값, 모션 파라미터 편집
  // Robot 탭: 포지션별 속도 편집 (RobotVelocityEditRow 컬렉션)
  + RecipeNames: ObservableCollection<string>
  + SelectedRecipeName: string?     (선택 시 자동 로드)
  + RobotVelocityRows: ObservableCollection<RobotVelocityEditRow>
  + Cmd_Reload / Cmd_New / Cmd_Save / Cmd_SaveAs

RobotVelocityEditRow                extends NotifyPropertyChangedBase
  + PositionName: string
  + Velocity: string                (편집 가능)
  + VelocityInt: int                (1-100 클램핑)

ManualViewModel                     extends NotifyPropertyChangedBase
  + Motor: Manual_MotorViewModel
  + Robot: Manual_RobotViewModel

Manual_MotorViewModel               extends NotifyPropertyChangedBase
  - _activeCts: CancellationTokenSource?   (이동/홈/서보)
  - _jogCts: CancellationTokenSource?      (조그 전용)
  - _statusTimer: DispatcherTimer          (200ms 폴링)
  + U/V/W/ZLoad/ZBond: string            (레시피 연동 티칭값)
  + SelectedAxis: string
  + JogSpeed: string
  + AxisStatuses: ObservableCollection<AxisStatusItem>
  + Cmd_Move*/Teach*/Stop*/ServoOn*/ServoOff*/Home*
  + Cmd_JogPlusDown/Up / JogMinusDown/Up
  + Cmd_StopAllMotors

  AxisStatusItem                    extends NotifyPropertyChangedBase (중첩)
    + AxisName, Position, IsServoOn, IsHome, IsMoving,
      IsFault, IsLimitPos, IsLimitNeg

Manual_RobotViewModel               extends NotifyPropertyChangedBase
  // TCP 로봇 연결/이동 수동 제어
```

---

## 8. Tcd.Robot.Simulator — 외부 TCP 서버

```
Program
  // 진입점: SimulatorServer 시작

SimulatorServer
  // TcpListener, 클라이언트 연결 수락

ClientSession
  // 단일 클라이언트 처리 (JSON 프로토콜)
  // MoveAsync, SetVelocity, Stop, Home 커맨드 처리

RobotSimCore
  // 로봇 상태 머신 (포지션, 이동 중, 에러)
  // TeachTable 참조로 이동 시간 계산

RobotMessages                       (Tcd.Robot.Simulator.Protocol)
  // 요청/응답 JSON DTO

DefaultTeachTable
TeachPoint
  // 포지션 → 이동 시간 매핑 기본값
```

---

## 9. 핵심 설계 패턴 요약

| 패턴                    | 적용 위치                                               | 설명                                           |
| --------------------- | --------------------------------------------------- | -------------------------------------------- |
| Composition Root      | `MainCore`                                          | 단일 진입점에서 모든 의존성 조립                           |
| Interface Segregation | `IMotionService`, `IAxisStateProvider`              | 명령/상태 읽기 분리                                  |
| Singleton             | `MainCore.Instance`                                 | 앱 전역 공유 상태                                   |
| Command Pattern       | `ISequence` / `SequenceManager`                     | 모든 동작을 키로 실행                                 |
| Strategy              | `IMotionService` (Sim vs Spii)                      | 설정에 따라 구현체 교체                                |
| Observer              | `AlarmManager.AlarmRaised`, `SequenceManager.Trace` | UI 이벤트 구독                                    |
| MVVM                  | 전체 UI                                               | `NotifyPropertyChangedBase` + `RelayCommand` |
| Immutability          | `Material`, `SequenceResult`, `Alarm`               | 상태 변이 방지                                     |
| Polling Cache         | `AxisState`, `DispatcherTimer`                      | UI 스레드에서 200ms 폴링                            |

---

## 10. 주요 데이터 흐름

### 자동 시퀀스 실행

```
MainWindowViewModel.Cmd_StartAutoCommand
  → RunSequenceAsync("AUTO_Run", ...)
    → Task.Run: SequenceManager.RunAsync("AUTO_Run", _sim, null, ct)
      → AutoRunSequence.ExecuteAsync(context, ...)
        → SEMI_LoadUpperFilm → SEMI_LoadLowerFilm → SEMI_AlignUVW
          → SEMI_Bond → SEMI_UnloadProductToStage2
      → SequenceResult 반환
    → Dispatcher.Invoke: UI 상태 업데이트
```

### 레시피 저장

```
RecipeViewModel.Cmd_Save
  → BuildFromEditor(): TcdRecipe
  → IRecipeRepository.Save(recipe)
  → RecipeStore.Current = recipe
  → RecipeStore.CurrentChanged 이벤트
    → MainWindowViewModel 구독 → CurrentRecipeName 갱신
```

### 수동 모터 이동

```
Manual_MotorViewModel.Cmd_MoveU
  → RunOperation("Manual_Motor_U_AbsMove", "U", "Move")
    → SequenceManager.RunAsync
      → DelegateSequence (IMotionService.AbsMoveAsync 호출)
        → SimMotionService → SimAxis.CommandMoveToAsync
          → SimAxis.WaitForInPositionAsync (폴링)
```

---

## 11. 축 인덱스 매핑

| 논리명    | 인덱스 | 역할                  |
| ------ | --- | ------------------- |
| U      | 0   | Lower Chamber X 정렬축 |
| V      | 1   | Lower Chamber Y 정렬축 |
| W      | 2   | Lower Chamber 회전축   |
| ZLower | 3   | Lower Chamber Z 합착축 |
| ZUpper | 4   | Upper Chamber Z 합착축 |

`AxisDefine` (`Tcd.App.Core.Define`)에 상수로 선언. 하드코딩 금지.
