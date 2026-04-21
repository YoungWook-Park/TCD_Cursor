# SPEC: 모델 플로우 디스크립터 (메인 고정 + 주입)

| 항목 | 내용 |
|------|------|
| 관련 | [SPEC_Sequence_Engine.md](SPEC_Sequence_Engine.md) |

---

## 1. 목적

- **메인 시퀀스 그래프**는 모든 모델에서 동일.
- **모델·레시피 차이**는 `IModelFlowDescriptor`(또는 동등 인터페이스)로 주입.

---

## 2. `IModelFlowDescriptor` (예시 필드)

| 멤버 | 설명 |
|------|------|
| `ModelId` | `CGO_Line`, `RMA` 등 |
| `SkipPeel` | 박리 스텝 분기 |
| `BondTimeoutScale` | 타임아웃 배수(선택) |
| `UpperBondLimitMm` | 상부 합착 한계(인터락 연동, 선택) |
| `ActiveRecipeId` | 현재 레시피 |

스텝은 `context.Model`만 읽고 분기; **거대한 `IMainSeq` 구현 클래스 남발**은 지양.

---

## 3. 조립

- 앱 시작 또는 **레시피 적용** 시 `IModelFlowDescriptor` 인스턴스 생성 → `SequenceContext`에 설정.
- `MainSeqMachine`/`SequenceHost`는 그래프 실행만 담당.

---

## 4. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안 |
