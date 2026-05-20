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

### 축 티칭

| 필드 | 단위 | 설명 |
|------|------|------|
| `Motor_*_Ready` | mm | 공통 Ready 위치 |
| `Motor_UpperChamber_Bond` | mm | 상부 챔버 합착 위치 |
| `Motor_LowerChamber_Bond` | mm | 하부 챔버 합착 위치 |

### 로봇 티칭 포즈 (X_mm, Y_mm, Theta_deg)

| 포즈 | 용도 |
|------|------|
| `Robot_Ready` | 경유 대기 위치 |
| `S1_Pick` | Stage1 픽업 |
| `S2_Pick` | Stage2 픽업 |
| `UpperChamber_Place` | 상부 챔버 안착 |
| `LowerChamber_Place` | 하부 챔버 안착 |
| `LowerChamber_Pick` | 합착체 픽업 |
| `Stage_Place` | Stage2 배치 |

### 속도 설정

| 필드 | 단위 | 설명 |
|------|------|------|
| `RobotVelocity_*` | % | 포지션별 이동 속도 (기본값: `RobotVelocityDefault`) |
| `AxisVelocity` | mm/s | SPiiPlus 축 이동 속도 |
| `AxisAcc/Dec/Jerk` | mm/s² | 가감속 프로파일 |

### 기타

| 필드 | 설명 |
|------|------|
| `SkipPeel` | true 시 박리 스텝 생략 |
| `ESC_VoltageSet` | ESC 설정 전압 (V) |
| `Thickness_CGO_um` | 상부 필름 두께 (μm) |
| `Thickness_OCA_um` | 하부 필름 두께 (μm) |

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
