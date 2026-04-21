# SPEC: 시퀀스 엔진 (Manual / Semi / Auto, 병렬, 프리플라이트)

| 항목 | 내용 |
|------|------|
| 관련 | [SPEC_Sequence.md](SPEC_Sequence.md), [SPEC_Model_Flow.md](SPEC_Model_Flow.md) |
| C# 정의·스텝 id | [CONVENTIONS_Hmi_Sequences.md](CONVENTIONS_Hmi_Sequences.md) (`Vcd.Contracts.Define.SequenceDef`) |

---

## 1. 핵심 타입

### 1.1 `SequenceContext`

- `StartParameters` (IReadOnlyDictionary 또는 강한 타입, 시작 시 스냅샷 고정)
- `PreviousStepId`, `PreviousResult` (`Succeeded` / `Failed` / `Cancelled`)
- `CancellationToken` (UI Stop)
- `IModelFlowDescriptor` (모델별 SkipPeel, 타임아웃 등)
- `IIoMapClient` / 서비스 래퍼
- `ICycleLog` 또는 `ILogger`

### 1.2 `ISequenceStep`

- `string Id { get; }`
- `Task<StepResult> ExecuteAsync(SequenceContext context);`

### 1.3 그래프 노드 (JSON)

| `type` | 의미 |
|--------|------|
| `sequence` | 자식 노드를 순서대로 실행 |
| `parallel` | 자식 전부 `Task.WhenAll`; **Fail-fast** 또는 **전부 완료 후 Fault 집계** (정책 선택, 문서화) |
| `ref` | 다른 시퀀스 정의 ID를 한 블록으로 호출 |
| `step` | 단일 `ISequenceStep` Id (DI 레지스트리 해석) |

---

## 2. SequenceManager

- `RunAsync(string definitionId, SequenceStartParameters parameters, CancellationToken ct)`
- 정의 로드: `sequences/auto/Main.json`, `semi/…`, `manual/…` 등
- **Manual**: 단일 `step` 또는 짧은 `sequence`; **Semi**: 복합 `ref`; **Auto**: 전체 `Main`

---

## 3. 오토 메인 프리플라이트

**첫 노드(또는 전용 `step`)**: `Step_PreFlightChecks`

1. PLC 연결·통신 OK  
2. `DI_LowerChamberAtReady`, `DI_UpperChamberAtReady` (및 정의된 기타 Ready)  
3. `DI_LowStageVacSensor`, `DI_HighStageVacSensor` (스테이지 진공)  
4. 실패 시 `Failed` + 메시지, 시퀀스 중단  

Semi/Manual 그래프는 이 노드를 생략 가능.

---

## 4. 병렬 모션 (상·하 챔버)

- `parallel` 노드 아래 `step` 두 개: `MoveUpperChamberToBond`, `MoveLowerChamberToBond`  
- 구현체 내부에서 `Task.WhenAll` + 공통 `CancellationToken`  
- 인터락: 실행 전·중 `InterlockService` — [SPEC_Interlocks.md](SPEC_Interlocks.md)

---

## 5. 파일 예시 (`sequences/auto/Main.json`)

```json
{
  "id": "AutoMain",
  "root": {
    "type": "sequence",
    "children": [
      { "type": "step", "id": "Step_PreFlightChecks" },
      { "type": "ref", "id": "Semi_LoadAndBond" }
    ]
  }
}
```

---

## 6. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안 |
