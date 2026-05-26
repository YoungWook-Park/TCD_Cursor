# SPEC: 설비 레이아웃·레시피 데이터 모델

---

## 설비 레이아웃

- **셀 크기**: 2000 × 2000 × 2500 mm
- **원점**: 바닥 왼쪽 앞 모서리 (0,0,0); X→우, Y→후방, Z→상방
- **좌측**: 상부 챔버 (ZUpper축) / 하부 챔버 (ZLower + UVW 3축)
- **우측 하단**: Stage1 (상부 필름), Stage2 (하부 필름)
- **우측**: 로봇 베이스 중심 (1200, 1000)

---

## 로봇 모델

- 자유도: **X, Y, θ** (갠트리식, IK 없음)
- 위치 진실: PLC 맵 `RobotPos_X/Y/Theta` (또는 TCP 시뮬 상태)

---

## 레시피 데이터 모델 (`TcdRecipe`)

> 클래스 전체 정의는 `CLASS_DESIGN.md` Section 5-2 참고.
> 실제 구현: `Tcd.App/Core/Recipes.cs`

### 축 티칭 (`AxisTeach: Dictionary<string, double>`)

키: `"U"`, `"V"`, `"W"`, `"ZLower"`, `"ZUpper"` (단위: mm)

| 논리 용도 | 키 예시 | 의미 |
|---------|---------|------|
| Load 위치 | 티칭 후 저장 | 챔버 개방(대기) 위치 |
| Bond 위치 | 티칭 후 저장 | 합착 이동 위치 |

### 로봇 티칭 (`RobotTeach: Dictionary<string, RobotPosition>`)

포지션명(키)은 `RobotPositionName` 상수 참고 (`Tcd.App/Define/Robot/RobotDefine.cs`).

### 속도 설정

| 필드 | 단위 | 설명 |
|------|------|------|
| `RobotVelocity` | % | 포지션별 이동 속도 (`RobotVelocityDefault` 기본값) |
| `MotionVelocity` | mm/s | SPiiPlus 축 이동 속도 |
| `MotionAcc/Dec/Jerk` | mm/s² | 가감속 프로파일 |

---

## 티칭 좌표 초기 템플릿 (mm, deg)

| 포즈 | X | Y | θ |
|------|---|---|---|
| Robot_Ready | 1200 | 1000 | 0 |
| S1_Pick | 1700 | 1300 | 90 |
| S2_Pick | 1700 | 700 | 90 |
| UpperChamber_Place | 550 | 1000 | 0 |
| LowerChamber_Place | 550 | 1000 | 0 |

3D 배치 확인 후 조정.
