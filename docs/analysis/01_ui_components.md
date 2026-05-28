# 1. UI 화면 구성 — 레시피·세팅·디바이스

TCD_Cursor HMI의 화면 구성과 각 페이지의 역할을 정리한다.

---

## 목차

- [네비게이션 구조](#네비게이션-구조)
- [메인 화면](#메인-화면)
- [레시피](#레시피)
- [매뉴얼](#매뉴얼)
- [디바이스 설정](#디바이스-설정)
- [앱 설정](#앱-설정)

---

## 네비게이션 구조

단일 `MainWindow`의 `ContentPresenter`에 뷰모델을 교체하는 방식으로 페이지 전환을 구현한다.
별도 `Frame` / 라우팅 없이 `CurrentContent` 프로퍼티 하나로 전체 네비게이션을 관리한다.

```
MainWindow
  └── ContentPresenter (CurrentContent 바인딩)
        ├── Main      — 장비 개략도 + 자동 사이클 제어
        ├── Recipe    — 레시피 편집·저장
        ├── Manual    — 수동 축 제어
        ├── Robot     — 로봇 상태 모니터링
        ├── Device    — 하드웨어 연결 관리
        └── Settings  — 앱 환경설정
```

각 페이지는 `MainWindowViewModel`이 생성 시점에 미리 인스턴스화하며,
네비게이션 커맨드 실행 시 해당 인스턴스를 `CurrentContent`에 대입한다.

---

## 메인 화면

장비 전체 상태를 한눈에 파악하고 자동 사이클을 제어하는 기본 화면이다.

### 장비 개략도

평면도(top-down) 기준 레이아웃:

| 위치 | 구성요소 |
|------|----------|
| 좌상단 | Upper Chamber (ZUpper 축) |
| 좌하단 | Lower Chamber (ZLower + UVW 축) |
| 우상단 | Robot |
| 우하단 | Stage 1 / Stage 2 |

본딩 진행 중(`IsBonding = true`)일 때 Upper Chamber는 ↓, Lower Chamber는 ↑ 방향으로 애니메이션이 동작한다.
두 챔버는 독립된 모터 모듈이며 공유 Z축이 없다.

### 자재 상태 표시

200ms 주기 `DispatcherTimer`가 시뮬레이션 상태를 폴링해 아래 4개 위치의 자재 유무를 갱신한다.

- Stage 1 / Stage 2
- Upper Chamber / Lower Chamber

### 제어 버튼

| 버튼 | 동작 |
|------|------|
| Load Stage | Stage 1·2에 UpperFilm / LowerFilm 자재 생성 |
| Start Auto | `AUTO_Run` 시퀀스 실행 (전체 사이클) |
| Stop | 실행 중인 시퀀스 취소 |
| Unload | `SEMI_UnloadProductToStage2` 시퀀스 실행 |
| Clear | 시뮬레이션 초기화 + 알람 로그 클리어 |

`IsRunning` 상태에 따라 Start·Load·Unload·Clear는 비활성화, Stop은 활성화된다.

### 알람·트레이스 로그

`SequenceManager.Trace` 이벤트와 `AlarmManager.AlarmRaised` 이벤트를 동일한
`Alarms` 컬렉션에 삽입(최신 항목이 맨 위)한다.

> **개선 예정**: 시퀀스 트레이스 로그와 알람을 별도 컬렉션으로 분리할 계획이다.

---

## 레시피

공정 파라미터를 JSON 파일로 저장·관리하는 페이지다.
`Motor` 탭과 `Robot` 탭으로 구성된다.

### Motor 탭 — 축 위치 및 모션 파라미터

| 항목 | 설명 |
|------|------|
| U / V / W | 하부 챔버 정렬 위치 (mm) |
| ZLoad | 하부 챔버 로딩 높이 (mm) |
| ZBond | 상부 챔버 본딩 높이 (mm) |
| Velocity | 이동 속도 (%) |
| Acc / Dec / Jerk | 가감속 프로파일 |

### Robot 탭 — 포지션별 이동 속도

로봇이 경유하는 11개 위치 각각에 대해 이동 속도(1~100%)를 개별 설정한다.

| 포지션 | 설명 |
|--------|------|
| Home | 원점 대기 |
| Ready | 작업 준비 위치 |
| S1_PickupWait / S1_Pick | Stage 1 픽업 대기·접근 |
| S2_PickupWait / S2_Pick | Stage 2 픽업 대기·접근 |
| UpperChamber_PickupWait / Pick | 상부 챔버 픽업 대기·접근 |
| LowerChamber_PickupWait / Pick | 하부 챔버 픽업 대기·접근 |
| Peel | 박리 위치 |

### 레시피 관리

| 커맨드 | 동작 |
|--------|------|
| Reload | 저장 폴더에서 목록 재로드 |
| New | 기본값으로 새 레시피 생성 |
| Save | 현재 편집 내용을 동일 이름으로 저장 |
| Save As | 이름이 같으면 `_Copy` 접미사 붙여 새 파일로 저장 |

레시피 파일은 `RecipeRepository`가 지정 디렉터리에 JSON으로 저장하며,
`MainCore.Recipes.Current`를 통해 시퀀스 실행 시 참조된다.

---

## 매뉴얼

디바이스 연결 상태 확인과 5축 수동 제어를 수행하는 페이지다.

### 통신 상태 바

상단에 3개 디바이스의 연결 상태를 LED + 텍스트로 표시한다.
`DispatcherTimer` 500ms 주기로 폴링하여 상태를 갱신한다.

| 디바이스 | 연결 상태 표시 |
|----------|----------------|
| SPiiPlus | Connected / Simulation |
| Robot | Connected / Disconnected |
| PLC | Connected / Disconnected |

> 실제 연결·해제 조작은 **Device** 페이지에서 수행한다.

### Motor 탭 — 5축 수동 제어

U / V / W / ZLower / ZUpper 5개 축에 대해 아래 8가지 조작을 지원한다.

| 커맨드 | 설명 |
|--------|------|
| AbsMove | 입력 위치로 절대 이동 |
| IncMove | 현재 위치 기준 상대 이동 |
| JogMove | 버튼 누르는 동안 지속 이동 |
| Stop | 현재 이동 중지 |
| Home | 원점 복귀 |
| FaultReset | 축 에러 클리어 |
| ServoOn / ServoOff | 서보 활성화·비활성화 |

각 조작은 `SequenceManager`를 통해 해당 축의 Manual 시퀀스 키로 실행된다.
Jog는 버튼 Press/Release 이벤트로 별도 `CancellationTokenSource`를 관리한다.

---

## 디바이스 설정

3개 하드웨어 디바이스의 연결을 탭으로 구분하여 관리한다.
설정값은 `device.json`에 저장된다(`AppSettings`와 분리).

| 탭 | 내용 |
|----|------|
| SPiiPlus | IP 주소 입력, 연결·해제 |
| Robot | TCP 호스트·포트, 연결·해제 |
| PLC | 연결 설정, 연결·해제 |

> `AppSettings`에서 하드웨어 연결 항목을 분리하여 `DeviceSettings`로 독립시킨 리팩토링 결과물이다.
> 환경설정(타임아웃, 로그 경로)과 디바이스 설정의 관심사를 분리하는 것이 목적이었다.

---

## 앱 설정

장비 연결과 무관한 애플리케이션 환경설정을 관리한다.

| 항목 | 설명 |
|------|------|
| 로그 저장 경로 | CSV 로그 파일 저장 디렉터리 |
| Stage Load Timeout | 스테이지 로딩 대기 최대 시간 (초) |
| Robot Move Timeout | 로봇 이동 완료 대기 최대 시간 (초) |
| Axis Move Timeout | 축 이동 완료 대기 최대 시간 (초) |

설정은 런타임에 즉시 반영되며, `MainCore.Instance.Settings`를 통해 각 시퀀스에서 참조한다.
