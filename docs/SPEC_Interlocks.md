# SPEC: 인터락

> 현재 일부 구현 (시퀀스 내 인터락 조건). PC 전체 `InterlockService`는 미구현.

---

## 원칙

- **PLC 맵**이 최종 허용/차단
- **PC** `InterlockService`는 명령 전 동일 규칙으로 선검사 → 불필요한 Fault 방지

---

## 현재 구현된 인터락 (시퀀스 내)

| 시퀀스 | 조건 | 위반 시 |
|--------|------|---------|
| `SEMI_LoadUpperFilm` | UpperChamber 비어있어야 함 | `Alarm(ChamberNotEmpty)` + Fail |
| `SEMI_LoadLowerFilm` | LowerChamber 비어있어야 함 | `Alarm(ChamberNotEmpty)` + Fail |
| `SEMI_AlignUVW` | Robot @ Home | `Alarm(RobotNotAtHome)` + Fail |

---

## 추가 예정 규칙

| RuleId | 조건 |
|--------|------|
| `PumpRequiresBothBond` | 진공펌프 On → 양 챔버 합착 위치 모두 도달 |
| `LowerBlockedIfUpperPastBond` | 상부 위치 > 한계 → 하부 이동 거부 |
| `MoveRequiresAtmosphericOrSafe` | 벤트·대기압 미달 → 챔버 Z 이동 금지 |

---

## `IInterlockRule` 인터페이스 (계획)

```csharp
interface IInterlockRule {
    string RuleId { get; }
    InterlockResult Evaluate(MotionIntent intent, EquipmentSnapshot snapshot);
}
```
