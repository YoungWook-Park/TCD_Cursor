# SPEC: 시퀀스 구조 및 흐름

코드 위치: `Tcd.App/Sequences/`, `Tcd.Simulator/TcdSequenceRegistry.cs`

---

## 계층 구조

```
Auto          — 전체 사이클 오케스트레이터 (AutoRunSequence.cs)
SemiAuto      — 단계별 복합 시퀀스 5종
Manual        — 축별 단일 조작 (5축 × 8개)
Atomic        — DelegateSequence 원자 단위 (TcdSequenceRegistry.cs)
```

모든 시퀀스는 `SequenceManager.RunAsync(key, context, param, ct)` 단일 경로로 실행된다.  
키 상수: `Tcd.Simulator/TcdSequenceKeys.cs`

---

## AUTO 시퀀스 흐름

```
1. PLC_Wait_StageLoaded (5s 타임아웃)
2. SEMI_LoadUpperFilm
   ├─ 인터락: UpperChamber 비어있어야 함
   ├─ Robot → Stage → Pick(Stage1) → UpperLoad → Place(UpperChamber)
3. SEMI_LoadLowerFilm
   ├─ 인터락: LowerChamber 비어있어야 함
   └─ Robot → Stage → Pick(Stage2) → LowerLoad → Place(LowerChamber)
4. Robot_Move_Home + Robot_Wait_Home (2s)
5. SEMI_AlignUVW
   ├─ 인터락: Robot @ Home
   ├─ Fork: U/V/W 동시 Command(0)
   └─ Join: U/V/W 동시 Wait(in-position, 2s)
6. SEMI_Bond
   ├─ Z: Command → 100(Bond위치) → Wait(3s)
   ├─ Dwell 1s
   ├─ Z: Command → 0(Load위치) → Wait(3s)
   └─ Material_Create_Bonded
7. SEMI_UnloadProductToStage2
   └─ Robot → LowerLoad → Pick(LowerChamber) → Stage → Place(Stage2)
→ Success
```

---

## Manual 시퀀스

5축 (U / V / W / ZLower / ZUpper) 각 8가지 조작:

| 조작 | 설명 |
|------|------|
| AbsMove | 레시피 티칭 위치로 절대 이동 |
| IncMove | `param(double)` 만큼 증분 이동 |
| JogMove | `param(double)` 속도로 조그 (CT 취소 시 정지) |
| Stop | 즉시 정지 |
| Home | 홈 복귀 |
| FaultReset | 폴트 클리어 |
| ServoOn / ServoOff | 서보 제어 |

키 패턴: `Manual_Motor_{Axis}_{Operation}` (예: `Manual_Motor_U_IncMove`)

---

## Atomic 시퀀스 목록

| 그룹 | 예시 키 |
|------|---------|
| Robot 이동 | `Robot_Move_Stage`, `Robot_Move_Home`, `Robot_Move_UpperLoad`, `Robot_Move_LowerLoad` |
| Robot 대기 | `Robot_Wait_Stage`, `Robot_Wait_Home`, … |
| Robot Pick/Place | `Robot_Pick_Stage1`, `Robot_Place_UpperChamber`, … |
| Axis Command | `AxisU_Command_Zero`, `AxisZ_Command_Bond`, `AxisZ_Command_Load` |
| Axis Wait | `AxisU_Wait_Zero`, `AxisZ_Wait_Bond`, `AxisZ_Wait_Load` |
| PLC | `Plc_Wait_StageLoaded` |
| 자재 | `Material_Create_Bonded` |
| 기타 | `Delay_Bond_Dwell1s`, `Sequence_Init` |

---

## SequenceResult

| Status | 의미 | 발생 조건 |
|--------|------|----------|
| `Succeeded` | 정상 완료 | — |
| `Failed` | 실패 | 예외 또는 인터락 위반 → Alarm 발생 |
| `Stopped` | 사용자 정지 | `OperationCanceledException` |
