# SPEC: 인터락 (PC 선검사 + PLC 최종)

| 항목 | 내용 |
|------|------|
| I/O | [SPEC_Control_IO.md](SPEC_Control_IO.md) |

---

## 1. 원칙

- **PLC 맵**이 최종 허용/차단.
- **PC** `InterlockService`는 명령 전 **동일 규칙**으로 선검사 → 불필요한 Fault·UX 개선.

---

## 2. `EquipmentSnapshot`

PLC 맵에서 읽은 읽기 전용 스냅샷 (주기 갱신):

- 챔버 압력(kPa 스케일), `DI_*AtBond`, `DI_*AtReady`, `DO_VentValveOpen` / `DI_AtAtmospheric`
- 상·하 챔버 위치(mm, 시뮬이 제공 시)
- 펌프 Run, E-Stop, 도어 등

---

## 3. `IInterlockRule`

- `string RuleId { get; }`
- `InterlockResult Evaluate(MotionIntent intent, EquipmentSnapshot s);`
- `MotionIntent`: 축/명령 종류(PumpOn, MoveLowerZ, VentOpen, …), 목표값

---

## 4. 규칙 예 (문서·코드 동기)

| RuleId | 조건(개략) |
|--------|------------|
| `PumpRequiresBothBond` | `DO_VacPumpRequest` 허용 → `UpperAtBond && LowerAtBond` |
| `LowerBlockedIfUpperPastBond` | 상부 위치 > 레시피 한계 → 하부 이동 명령 거부 |
| `MoveRequiresAtmosphericOrSafe` | 벤트·대기압 미달 시 챔버 Z 이동 등 금지 (`DI_AtAtmospheric` 또는 압력 임계) |

---

## 5. 시퀀스·병렬

- `Task.WhenAll` 전 각 축 `AssertMove`; 장시간 이동 중에는 주기적 스냅샷 재검사 권장.

---

## 6. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안 |
