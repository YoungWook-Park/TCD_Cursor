# SPEC: 레이아웃·좌표계·레시피

| 항목 | 내용 |
|------|------|
| 관련 PRD | [PRD_Lamination_Simulator.md](PRD_Lamination_Simulator.md) |

---

## 1. 설비 셀

- **크기**: 가로 **2000 mm** × 세로 **2000 mm** × 높이 **2500 mm**.
- **원점**: 바닥 **왼쪽 앞** 모서리 `O = (0, 0, 0)`; **X** 우측, **Y** 후방(도면 세로), **Z** 상방.
- **3D UI 스케일**: 내부 저장·레시피는 **mm**, **deg**; WPF/Helix에서 `meter = mm / 1000`.

---

## 2. 블록 배치(초안 비율)

| 구역 | 범위 (mm) | 비고 |
|------|-----------|------|
| 챔버(상·하 개략) | X: 150~950, Y: 500~1500, Z: 400~2100 | 합착 Z는 레시피 모터값으로 이동 |
| 로봇 베이스 중심 | (1200, 1000), Z=0 | 기둥 높이 예: 400 |
| 하부 스테이지(OCA 로드 구역 예) | 플랫폼 상면 중심 **(1700, 700)**, 상면 Z=**850** | 우측 하단 칸 |
| 상부 스테이지(CGO 로드 구역 예) | 플랫폼 상면 중심 **(1700, 1300)**, 상면 Z=**850** | 우측 하단 인접 칸 |
| 합착 기준점(예시) | 하부 지지면 **(550, 1000, 880)** | 티칭 조정용 |

---

## 3. 로봇 단순화 모델

- **구동 자유도**: **X, Y, θ** 만 (갠트리/포털 + 수평 회전). IK 없음.
- **표시**: 3D 메시는 다관절처럼 보이게 할 수 있으나 **애니메이션은 `Translate`(X,Y) + `Rotate`(θ)** 만 사용.
- **진실 위치**: PLC 맵 `RobotPos_X_mm`, `RobotPos_Y_mm`, `RobotPos_Theta_mdeg` ([SPEC_Control_IO.md](SPEC_Control_IO.md)); WPF는 목표를 쓰거나 시뮬이 추종하도록 정책 선택.

---

## 4. 레시피 화면 — 데이터 모델(필드)

모델(제품 타입)마다 **레시피 ID**로 저장·전환. 공통 구조 예시:

### 4.1 공통

| 필드 | 타입 | 설명 |
|------|------|------|
| `RecipeId` | int | 식별자 |
| `ModelName` | string | 모델명 |
| `SkipPeel` | bool | true 시 박리 스텝 생략 |

### 4.2 모터(챔버 축 등)

각 축 또는 그룹:

| 필드 | 단위 | 설명 |
|------|------|------|
| `Motor_*_Ready` | mm 또는 pulse | **공통 Ready** 위치 |
| `Motor_UpperChamber_PreBond` | mm | 상부 챔버 **합착 전** 위치 |
| `Motor_UpperChamber_Bond` | mm | 상부 챔버 **합착** 위치 |
| `Motor_LowerChamber_PreBond` | mm | 하부 챔버 **합착 전** |
| `Motor_LowerChamber_Bond` | mm | 하부 챔버 **합착** |

*(실제 축 수에 맞게 행 추가; 이름은 구현 시 ENUM/DB 컬럼으로 매핑.)*

### 4.3 로봇 티칭 포즈 (X_mm, Y_mm, Theta_deg)

| 포즈 ID | 용도 |
|---------|------|
| `Robot_Ready` | 공통 Ready |
| `PickWait_LowStage` | OCA 스테이지 Pick 대기 |
| `Pick_LowStage` | OCA Pick |
| `PickWait_UpperChamber` | 상부 챔버 안착 전 대기 |
| `Place_UpperChamber` | CGO 상부 안착 |
| `Peel` | 박리 자세 |
| `PickWait_BondedOnLower` | 합착체 Pick 대기(하부) |
| `Pick_BondedOnLower` | 합착체 Pick |
| `Place_LowStage` | 하부 스테이지 배치 |

레시피에는 위 각각에 **X, Y, θ** 저장.

### 4.4 ESC Chuck

| 필드 | 단위 | 설명 |
|------|------|------|
| `ESC_VoltageSet` | V | 설정 전압 (맵 `AO_ESC_VoltageSet_x100` 와 연동) |
| (선택) `ESC_RampMs` | ms | 시뮬 램프 시간 |

### 4.5 Material

| 필드 | 단위 | 설명 |
|------|------|------|
| `Thickness_CGO_um` | μm | 상부 필름 두께 |
| `Thickness_OCA_um` | μm | 하부 필름 두께 |

합착 시 간극·UVW 보정 시뮬에 사용.

### 4.6 UVW 가상 보정(선택 레시피 기본값)

| 필드 | 단위 |
|------|------|
| `UVW_Default_X_um` | μm |
| `UVW_Default_Y_um` | μm |
| `UVW_Default_Theta_mdeg` | mdeg |

실행 시 PLC/WPF 워드 W23~W25에 반영 가능.

---

## 5. 티칭 좌표 예시값 (초기 템플릿, mm / deg)

| 포즈 | X | Y | θ |
|------|---|---|---|
| Robot_Ready | 1200 | 1000 | 0 |
| PickWait_LowStage | 1600 | 700 | 90 |
| Pick_LowStage | 1700 | 700 | 90 |
| PickWait_UpperChamber | 700 | 1000 | 0 |
| Place_UpperChamber | 550 | 1000 | 0 |
| Peel | 550 | 1000 | 45 |
| PickWait_BondedOnLower | 600 | 1000 | 0 |
| Pick_BondedOnLower | 550 | 1000 | 0 |
| Place_LowStage | 1700 | 700 | 90 |

3D 배치 확인 후 **50~100 mm** 단위로 조정.

---

## 6. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안: 2m 셀, 레시피 필드, 로봇 단순화 |
