# Unit Test Coverage

> 최종 실행: 2026-05-26
> 결과: **172 / 172 통과 (100%)**

---

## 프로젝트 구성

| 프로젝트 | 대상 레이어 | 프레임워크 |
|---|---|---|
| `Tcd.Tests.Shared` | 공통 테스트 인프라 (페이크) | netstandard2.0 |
| `Tcd.Engine.Tests` | 순수 도메인 로직 | net8.0, xUnit 2.9 |
| `Tcd.Simulator.Tests` | 인프로세스 시뮬레이션 | net8.0, xUnit 2.9 |

```
Tcd.Tests.Shared
  ├── Tcd.Engine.Tests  (103개)
  └── Tcd.Simulator.Tests  (69개)
```

---

## 공통 테스트 인프라 (`Tcd.Tests.Shared`)

### 페이크  클래스

| 클래스 | 인터페이스 | 역할 |
|---|---|---|
| `FakeTimeProvider` | `ITimeProvider` | `Delay` 즉시 완료, `Now` 수동 제어 |
| `BlockingFakeTimeProvider` | `ITimeProvider` | `Delay`가 `Release()` 호출 전까지 블로킹 — IsMoving 등 진행 중 상태 검증용 |
| `FakeAlarmSink` | `IAlarmSink` | `Raise`된 알람을 `List<Alarm>`으로 수집 |
| `FakeSequenceContext` | `ISequenceContext` | `FakeAlarmSink` + `FakeTimeProvider` 조합 |

**FakeTimeProvider 동작 원칙:**
- `Now`: `2026-01-01 00:00:00Z` 고정값 (수동 변경 가능)
- `Delay`: 취소 요청 시 `Task.FromCanceled`, 그 외 `Task.CompletedTask` 즉시 반환
- `Now`가 고정값이므로 `while (time.Now - start < timeout)` 루프가 즉시 종료됨 → 타임아웃 테스트는 `SystemTimeProvider + 짧은 실제 timeout` 조합 사용

---

## Tcd.Engine.Tests (103개)

### AlarmManagerTests — 9개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Raise_ValidAlarm_AppearsInSnapshot` | Raise 후 Snapshot에 포함 |
| 2 | `Raise_NullAlarm_ThrowsArgumentNullException` | null 방어 |
| 3 | `Raise_FiresAlarmRaisedEvent` | 이벤트 발화 |
| 4 | `Raise_EventArg_IsSameInstanceAsRaisedAlarm` | 이벤트 인자 동일성 |
| 5 | `Snapshot_BeforeAnyRaise_ReturnsEmptyList` | 초기 빈 상태 |
| 6 | `Snapshot_ReturnsIsolatedCopy_ExternalModifyDoesNotAffect` | `ToArray()` 독립 복사본 |
| 7 | `Raise_MultipleAlarms_AllAccumulateInSnapshot` | 누적 동작 |
| 8 | `Raise_StringOverload_PopulatesCodeAndMessage` | string 오버로드 |
| 9 | `Raise_ConcurrentCalls_AllAlarmsAppearInSnapshot` | 10스레드 × 50알람 = 500개 `lock` 안전성 |

### SequenceManagerTests — 14개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Register_ValidSequence_ContainsByKey` | 등록 후 Contains = true |
| 2 | `Register_NullSequence_ThrowsArgumentNullException` | null 방어 |
| 3 | `Register_DuplicateKey_ReplacesExistingSequence` | `_sequences[key] = value` 덮어쓰기 |
| 4 | `Contains_UnregisteredKey_ReturnsFalse` | 미등록 키 |
| 5 | `Contains_RegisteredKey_ReturnsTrue` | 등록 키 |
| 6 | `List_EmptyManager_ReturnsEmptyCollection` | 초기 상태 |
| 7 | `List_AfterTwoRegistrations_ReturnsBothSequences` | 목록 반환 |
| 8 | `RunAsync_NullKey_ThrowsArgumentNullException` | null 방어 |
| 9 | `RunAsync_NullContext_ThrowsArgumentNullException` | null 방어 |
| 10 | `RunAsync_UnknownKey_ThrowsKeyNotFoundException` | 미등록 키 예외 |
| 11 | `RunAsync_ReturnsSucceededForSuccessSequence` | 정상 실행 결과 |
| 12 | `RunAsync_FiresStartedTraceEvent` | Trace 이벤트 Started |
| 13 | `RunAsync_FiresCompletedTraceEventWithResult` | Trace 이벤트 Completed + Status |
| 14 | `RunAsync_KeyLookupIsCaseInsensitive` | `OrdinalIgnoreCase` 동작 |

### DelegateSequenceTests — 10개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullKey_ThrowsArgumentNullException` | null 방어 |
| 2 | `Constructor_NullBody_ThrowsArgumentNullException` | null 방어 |
| 3 | `Constructor_NullDisplayName_DefaultsToKey` | displayName 기본값 |
| 4 | `Key_And_DisplayName_ReturnConstructorValues` | 프로퍼티 반환값 |
| 5 | `ExecuteAsync_BodySucceeds_ReturnsSucceeded` | 정상 경로 → Succeeded |
| 6 | `ExecuteAsync_OperationCanceledException_ReturnsStopped` | 취소 → Stopped |
| 7 | `ExecuteAsync_GeneralException_ReturnsFailed` | 예외 → Failed |
| 8 | `ExecuteAsync_OnException_RaisesAlarm` | FakeAlarmSink에 알람 추가 |
| 9 | `ExecuteAsync_OnException_AlarmCode_IsSEQ_ERROR` | 알람 코드 "SEQ_ERROR" |
| 10 | `ExecuteAsync_OnException_ErrorMessageMatchesExceptionMessage` | Error == ex.Message |

### ActionNodeTests — 10개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullId_ThrowsArgumentNullException` | null 방어 |
| 2 | `Constructor_NullAction_ThrowsArgumentNullException` | null 방어 |
| 3 | `RunAsync_ActionSucceeds_ReturnsSucceeded` | 정상 경로 |
| 4 | `RunAsync_ActionThrowsOperationCanceled_ReturnsStopped` | 취소 경로 |
| 5 | `RunAsync_ActionThrowsException_ReturnsFailed` | 예외 경로 |
| 6 | `RunAsync_OnException_RaisesAlarm_WithSEQ_ERROR_Code` | 알람 코드 "SEQ_ERROR" |
| 7 | `RunAsync_OnTimeoutException_RaisesAlarm_WithSEQ_TIMEOUT_Code` | 알람 코드 "SEQ_TIMEOUT" |
| 8 | `RunAsync_WithNoTimeout_ActionSucceeds_ReturnsSucceeded` | timeout=null 케이스 |
| 9 | `RunAsync_WithTimeout_CompletesBeforeDeadline_Succeeds` | 충분한 timeout |
| 10 | `RunAsync_WithTimeout_ExceedsDeadline_RaisesAlarmAndFails` | 1ms timeout + 무한 Task |

### DecisionNodeTests — 8개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullId_ThrowsArgumentNullException` | null 방어 |
| 2 | `Constructor_NullPredicate_ThrowsArgumentNullException` | null 방어 |
| 3 | `RunAsync_PredicateReturnsTrue_ReturnsSucceeded` | 조건 참 |
| 4 | `RunAsync_PredicateReturnsFalse_ReturnsFailed` | 조건 거짓 |
| 5 | `RunAsync_PredicateThrowsOperationCanceled_ReturnsStopped` | 취소 경로 |
| 6 | `RunAsync_PredicateThrowsException_RaisesAlarmAndReturnsFailed` | 예외 경로 |
| 7 | `RunAsync_WithTimeout_CompletesBeforeDeadline_Succeeds` | 시간 내 완료 |
| 8 | `RunAsync_WithTimeout_ExceedsDeadline_RaisesAlarmAndFails` | 1ms timeout + 무한 predicate |

### SequenceGraphTests — 10개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullStartNodeId_Throws` | null 방어 |
| 2 | `AddNode_NullNode_Throws` | null 방어 |
| 3 | `AddNode_DuplicateId_Throws` | Dictionary.Add 중복 키 예외 |
| 4 | `Contains_AfterAddNode_ReturnsTrue` | 노드 존재 확인 |
| 5 | `Contains_UnknownId_ReturnsFalse` | 미존재 확인 |
| 6 | `GetNode_KnownId_ReturnsNode` | 정상 조회 |
| 7 | `GetNode_UnknownId_ThrowsKeyNotFoundException` | 미존재 예외 |
| 8 | `TryGetNext_AfterSetNext_ReturnsTrueAndNodeId` | 엣지 등록 후 조회 |
| 9 | `TryGetNext_NoEdge_ReturnsFalse` | 엣지 없음 |
| 10 | `SetNext_OverwritesExistingEdge` | 덮어쓰기 동작 |

### SequenceRunnerTests — 12개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Run_SingleSuccessNode_ReturnsSucceeded` | 단일 노드 성공 |
| 2 | `Run_ChainedTwoNodes_BothExecute` | 체인 실행 (side-effect 확인) |
| 3 | `Run_ChainedThreeNodes_ExecuteInOrder` | 3노드 순서 보장 |
| 4 | `Run_FirstNodeFails_SecondNodeNotExecuted` | 실패 시 조기 중단 |
| 5 | `Run_FirstNodeFails_ReturnsFailedResult` | Failed 결과 전달 |
| 6 | `Run_NodeReturnsStopped_PropagatesStopped` | Stopped 전달 |
| 7 | `Run_CancellationBeforeStart_ReturnsStopped` | 사전 취소 토큰 |
| 8 | `Run_ForkNode_AllBranchesExecute` | 병렬 분기 모두 실행 |
| 9 | `Run_ForkNode_AllBranchesSucceed_ContinuesToJoinNext` | join 후 다음 노드 |
| 10 | `Run_ForkNode_OneBranchFails_PropagatesFailure` | 분기 실패 전달 |
| 11 | `Run_ForkNode_JoinNextIsNull_StopsAfterJoin` | JoinNext=null 종료 |
| 12 | `Run_NestedFork_AllBranchesSucceed_ReturnsSucceeded` | 중첩 fork 성공 |

### InMemoryMaterialTrackerTests — 24개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `IsOccupied_EmptyLocation_ReturnsFalse` | 초기 상태 |
| 2 | `IsOccupied_AfterPlace_ReturnsTrue` | Place 후 점유 |
| 3 | `Place_ValidMaterial_OccupiesLocation` | 정상 배치 |
| 4 | `Place_UpdatesMaterialLocationProperty_ToTarget` | `material.With(location:)` |
| 5 | `Place_NullMaterial_ThrowsArgumentNullException` | null 방어 |
| 6 | `Place_NoneLocation_ThrowsArgumentException` | `MaterialLocation.None` 방어 |
| 7 | `Place_AlreadyOccupied_ThrowsInvalidOperationException` | 중복 배치 방어 |
| 8 | `Get_OccupiedLocation_ReturnsMaterial` | 정상 조회 |
| 9 | `Get_EmptyLocation_ReturnsNull` | 미배치 조회 |
| 10 | `Remove_OccupiedLocation_ReturnsMaterial` | 정상 제거 |
| 11 | `Remove_OccupiedLocation_SlotBecomesEmpty` | 제거 후 미점유 |
| 12 | `Remove_ReturnedMaterial_HasNoneLocation` | `With(location:None)` |
| 13 | `Remove_EmptyLocation_ReturnsNull` | 미배치 제거 |
| 14 | `Move_TransfersMaterialFromSourceToDestination` | 이동 정상 |
| 15 | `Move_SourceBecomesEmpty` | 출발지 해제 |
| 16 | `Move_DestinationBecomesOccupied` | 목적지 점유 |
| 17 | `Move_MaterialLocation_UpdatedToDestination` | 위치 갱신 |
| 18 | `Move_FromEmpty_ThrowsInvalidOperationException` | 빈 출발지 방어 |
| 19 | `Move_ToOccupied_ThrowsInvalidOperationException` | 점유 목적지 방어 |
| 20 | `Move_NoneFromLocation_ThrowsArgumentException` | None 방어 |
| 21 | `Move_NoneToLocation_ThrowsArgumentException` | None 방어 |
| 22 | `Clear_RemovesAllMaterials` | 전체 초기화 |
| 23 | `Snapshot_ReturnsAllPlacedMaterials` | 전체 조회 |
| 24 | `Snapshot_ReturnsIsolatedCopy_ExternalModifyDoesNotAffect` | 독립 복사본 |

### TimeoutsTests — 6개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `WithTimeout_TaskAlreadyCompleted_DoesNotThrow` | 이미 완료된 Task |
| 2 | `WithTimeout_CompletesBeforeDeadline_DoesNotThrow` | 충분한 timeout |
| 3 | `WithTimeout_ExceedsDeadline_ThrowsTimeoutException` | 1ms timeout + 무한 Task |
| 4 | `WithTimeout_CancellationRequested_PropagatesCancellation` | 취소 시 `TimeoutException` 발생¹ |
| 5 | `WithTimeoutT_CompletesBeforeDeadline_ReturnsValue` | 제네릭 오버로드 값 반환 |
| 6 | `WithTimeoutT_ExceedsDeadline_ThrowsTimeoutException` | 제네릭 오버로드 타임아웃 |

> ¹ **구현 특이사항:** `CancellationToken`이 이미 취소된 상태로 전달되면 `CreateLinkedTokenSource`가 즉시 취소되어 내부 `timeoutTask`가 먼저 완료되므로, 취소 시에도 `TimeoutException`이 발생한다.

---

## Tcd.Simulator.Tests (69개)

### SimAxisTests — 14개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullName_Throws` | null 방어 |
| 2 | `Constructor_NullTimeProvider_Throws` | null 방어 |
| 3 | `Constructor_ZeroOrNegativeSpeed_DefaultsTo50` | 기본 속도 방어 |
| 4 | `InitialPosition_IsZero` | 초기 위치 0 |
| 5 | `IsMoving_Initial_IsFalse` | 초기 이동 상태 |
| 6 | `IsServoOn_DefaultIsFalse` | 초기 서보 상태 |
| 7 | `IsServoOn_SetTrue_ReturnsTrue` | 서보 수동 조작 |
| 8 | `CommandMoveToAsync_WithFakeTime_PositionUpdatedToTarget` | 이동 후 위치 갱신 |
| 9 | `CommandMoveToAsync_WithBlockingFake_IsMovingIsTrueWhileMoving` | 이동 중 IsMoving = true |
| 10 | `CommandMoveToAsync_WithFakeTime_IsMovingIsFalseAfterComplete` | 완료 후 IsMoving = false |
| 11 | `WaitForInPosition_AlreadyAtTarget_ReturnsImmediately` | 즉시 복귀 |
| 12 | `WaitForInPosition_WithFakeTime_AfterCommandMove_Succeeds` | 이동 후 대기 성공 |
| 13 | `WaitForInPosition_TimesOut_ThrowsTimeoutException` | `SystemTimeProvider` + 10ms timeout |
| 14 | `WaitForInPosition_Cancellation_ThrowsOperationCanceledException` | 취소 예외 |

### SimLowerChamberMotionTests — 6개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullTime_Throws` | null 방어 |
| 2 | `FourAxes_AllInitializedWithCorrectNames` | U/V/W/Z 이름 확인 |
| 3 | `CommandMoveToBondingPosition_WithFakeTime_AllAxesReachTarget` | U/V/W=0, Z=bondZ(100) |
| 4 | `WaitForBondingPosition_WithFakeTime_Succeeds` | 이미 본딩 위치 → 즉시 완료 |
| 5 | `CommandMoveToLoadPosition_WithFakeTime_ZReachesLoadZ` | Z=loadZ(0) |
| 6 | `WaitForLoadPosition_WithFakeTime_Succeeds` | 이미 로드 위치 → 즉시 완료 |

### SimRobotTests — 19개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullTimeProvider_Throws` | null 방어 |
| 2 | `Constructor_NullMaterials_Throws` | null 방어 |
| 3 | `InitialPosition_IsHome` | 초기 위치 Home |
| 4 | `HasVacuum_Initial_IsFalse` | 초기 진공 false |
| 5 | `CommandMoveToAsync_WithFakeTime_UpdatesCurrentPosition` | 이동 후 위치 갱신 |
| 6 | `WaitForPositionAsync_AlreadyAtTarget_ReturnsImmediately` | 즉시 복귀 |
| 7 | `WaitForPositionAsync_AfterCommandMove_WithFakeTime_Succeeds` | 이동 후 대기 성공 |
| 8 | `WaitForPositionAsync_TimesOut_ThrowsTimeoutException` | `SystemTimeProvider` + 10ms |
| 9 | `WaitForPositionAsync_Cancellation_ThrowsOperationCanceledException` | 취소 예외 |
| 10 | `PickAsync_MaterialAtLocation_SetsHasVacuumTrue` | 집기 후 HasVacuum = true |
| 11 | `PickAsync_RemovesMaterialFromTracker` | Tracker에서 제거 |
| 12 | `PickAsync_HeldMaterial_LocationIsRobot` | 보유 재료 위치 = Robot (HasVacuum 간접 확인) |
| 13 | `PickAsync_AlreadyHolding_ThrowsInvalidOperationException` | 이중 집기 방어 |
| 14 | `PickAsync_NoMaterialAtLocation_ThrowsInvalidOperationException` | 빈 위치 방어 |
| 15 | `PickAsync_Cancelled_ThrowsOperationCanceledException` | 취소 방어 |
| 16 | `PlaceAsync_HoldingMaterial_SetsHasVacuumFalse` | 놓기 후 HasVacuum = false |
| 17 | `PlaceAsync_PutsMaterialIntoTracker` | Tracker에 배치 |
| 18 | `PlaceAsync_NotHolding_ThrowsInvalidOperationException` | 빈손 방어 |
| 19 | `PlaceAsync_Cancelled_ThrowsOperationCanceledException` | 취소 방어 |

### SimPlcTests — 13개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullTime_Throws` | null 방어 |
| 2 | `Constructor_NullMaterials_Throws` | null 방어 |
| 3 | `ReadBitAsync_InitiallyFalse` | 초기 비트 false |
| 4 | `WriteBitAsync_SetTrue_ReadBitReturnsTrue` | 비트 쓰기/읽기 |
| 5 | `WriteBitAsync_SetFalse_AfterTrue_ReadBitReturnsFalse` | 비트 클리어 |
| 6 | `ReadWordAsync_InitiallyZero` | 초기 워드 0 |
| 7 | `WriteWordAsync_Value_ReadWordReturnsValue` | 워드 쓰기/읽기 |
| 8 | `ReadBitAsync_BitIndex_MapsToCorrectByteAndBit` | `addr = byteNum*8 + bitNum` 매핑 |
| 9 | `WaitForStageLoadedAsync_BothStagesOccupied_ReturnsTrue` | 양쪽 재료 있을 때 즉시 true |
| 10 | `WaitForStageLoadedAsync_NoMaterials_TimesOut_ReturnsFalse` | 재료 없음 → false |
| 11 | `WaitForStageLoadedAsync_OnlyStage1_ReturnsFalse` | Stage2 없으면 false |
| 12 | `WaitForStageLoadedAsync_Cancelled_ThrowsOperationCanceledException` | 취소 예외 |
| 13 | `StartAndStopMonitoring_NoOp_DoesNotThrow` | no-op 안전성 |

### SimMotionServiceTests — 17개

| # | 테스트 이름 | 검증 내용 |
|---|---|---|
| 1 | `Constructor_NullSimulation_Throws` | null 방어 |
| 2 | `Constructor_NullSettings_Throws` | null 방어 |
| 3 | `ServoOnAsync_SetsAxisIsServoOnTrue` | U축 ServoOn |
| 4 | `ServoOffAsync_SetsAxisIsServoOnFalse` | U축 ServoOff |
| 5 | `FaultClearAsync_CompletesWithoutException` | no-op 안전성 |
| 6 | `StopAsync_CompletesWithoutException` | no-op 안전성 |
| 7 | `GetAxisState_ReflectsCurrentSimAxisPosition` | 위치 반영 |
| 8 | `GetAxisState_ReflectsIsServoOn` | 서보 상태 반영 |
| 9 | `GetAxisState_IsHome_TrueWhenPositionNearZero` | `Math.Abs(pos) < 0.01` 판정 |
| 10 | `GetSnapshot_ReturnsExactlyFiveAxes` | 5축 반환 |
| 11 | `GetSnapshot_AxisOrder_IsUVWZLowerZUpper` | 축 순서 U/V/W/ZLower/ZUpper |
| 12 | `HomeAsync_MovesAxisToZeroPosition` | Home = 0 |
| 13 | `AbsMoveAsync_MovesAxisToTargetPosition` | 절대 이동 |
| 14 | `IncMoveAsync_IncreasesPositionByDelta` | 상대 이동 |
| 15 | `JogAsync_PositiveVelocity_PositionIncreases` | Jog 방향 확인 |
| 16 | `UnknownAxisName_ThrowsArgumentOutOfRangeException` | 미지원 축 방어 |
| 17 | `ZLower_And_ZUpper_BothMapToSameSimAxis` | ZLower/ZUpper 동일 SimAxis 매핑 기록² |

> ² **설계 메모:** 현재 `SimMotionService.Axis()`에서 "ZLower"와 "ZUpper" 모두 `_sim.LowerMotion.Z`를 반환한다. 실물 장비에서는 별도 축이므로 추후 개선 대상.

---

## 테스트 실행 명령

```bash
# Engine 단위 테스트
dotnet test Tcd.Engine.Tests/Tcd.Engine.Tests.csproj

# Simulator 단위 테스트
dotnet test Tcd.Simulator.Tests/Tcd.Simulator.Tests.csproj

# 전체 실행
dotnet test Tcd.Engine.Tests/Tcd.Engine.Tests.csproj
dotnet test Tcd.Simulator.Tests/Tcd.Simulator.Tests.csproj
```

---

## 커버리지 범위 외

다음은 현재 테스트되지 않는 영역입니다:

| 대상 | 이유 |
|---|---|
| `SemiAuto*Sequence` | `MainCore.Instance.LogContext = ...` 싱글턴 의존 — 인터페이스 주입으로 리팩터링 후 테스트 가능 |
| `AutoRunSequence` | 동일 이유 |
| `Manual_MotorViewModel` | `DispatcherTimer` + `MainCore.Instance` 의존 — WPF STA 스레드 필요 |
| `SpiiPlusMotionService` | 실제 ACS 하드웨어 필요 |
| `RobotTcpClient` / `PlcTcpClient` | TCP 서버 필요 (통합 테스트 영역) |
