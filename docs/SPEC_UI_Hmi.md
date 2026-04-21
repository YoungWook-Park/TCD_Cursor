# SPEC: WPF HMI (셸·레시피·티칭·매뉴얼·디바이스)

| 항목 | 내용 |
|------|------|
| 관련 | [PRD_Lamination_Simulator.md](PRD_Lamination_Simulator.md) |
| 구현 규약·폴더·네이밍 | [CONVENTIONS_Hmi_Sequences.md](CONVENTIONS_Hmi_Sequences.md) |
| 테마 구현 | `src/Vcd.Hmi.Wpf/Themes/DarkTheme.xaml` |

---

## 1. 창·테마

### 1.1 셸(메인 창)

- `WindowStyle="None"`, `ResizeMode="CanResize"`, `WindowChrome`로 커스텀 제목줄(드래그 이동).
- **레이아웃(고정)**: 상단 바 높이 40px · 중앙 콘텐츠 `*` · 하단 내비 56px. 컨트롤 **배치·순서는 변경하지 않는다**(요구 시 별도 항목으로 합의).
- 상단 오른쪽: **Start auto**, **Stop**(시퀀스), **Logs**, **최소화**, **종료** 순.
- 하단 내비: **Device** | **Recipe** | **Teach** | **Manual** | **3D**(모니터링). `ContentControl` + `DataTemplate`로 뷰 전환(MVVM).

### 1.2 시각 방향(산업용 HMI 다크)

- **참고**: 현장용 풀스크린 HMI — 남색 계열 베이스, 구역(상·하단 바) 구분, 가동 계열 **녹색**, 시퀀스 정지는 **무채/어두운 바**, 앱 종료·위험 동작은 **빨강**.
- **모서리**: 버튼·패널의 **둥근 모서리(CornerRadius)는 사용하지 않는다**(WPF 기본 직각). 추후 필요 시 별도 항목으로 추가.

### 1.3 색 토큰(`DarkTheme.xaml`)

| 키 | 용도 |
|----|------|
| `Color.Surface` / `Brush.Surface` | 창·메인 배경(남색 베이스) |
| `Color.SurfaceElevated` / `Brush.SurfaceElevated` | 상단·하단 바, 입력 배경 등 한 단계 올린 면 |
| `Color.Border` / `Brush.Border` | 테두리 |
| `Color.Text` / `Brush.Text` | 본문·라벨 |
| `Color.Accent` / `Brush.Accent` | 주요 긍정 동작(시작, 연결 등) — `AccentButton`, 토글/콤보 선택 강조 |
| `Color.Good` / `Brush.Good` | 정상/일치 피드백(예: 매뉴얼 진공 On 일치) |
| `Color.Danger` / `Brush.Danger` | 종료·긴급 정지 등 — `StopButton` |
| `Color.SequenceStop` / `Brush.SequenceStop` | **시퀀스 Stop** 전용(어두운 회색 톤; 앱 종료와 구분) |
| `Color.Warning` / `Brush.Warning` | 경고·알람 클리어 등(예약; 버튼 미연결 시에도 토큰만 유지 가능) |

색 값은 `DarkTheme.xaml`이 단일 출처(SoT)이다. 문서에 16진을 중복 적지 않는다.

### 1.4 버튼 스타일 키

| 스타일 | 용도 |
|--------|------|
| 기본 `TargetType="Button"` | 일반 동작·내비 |
| `AccentButton` | Start auto, 디바이스 Connect 등 **주요 긍정** |
| `SequenceStopButton` | 시퀀스 **Stop**만 |
| `StopButton` | **모터/축 Stop**, 창 **종료** 등 **위험·강한 중단** |

---

## 2. 하단 내비게이션

- 탭 또는 버튼 바: **Device** | **Recipe** | **Teach** | **Manual** | **3D**.
- 중앙 `ContentControl` + `DataTemplate`로 뷰 전환.

---

## 3. 레시피

- 경로: `D:\VCD_Recipe`, JSON 파일.
- 좌: 리스트; 우: `TabControl` 세부 항목.
- 컨텍스트 메뉴: **이 레시피 적용**, **복사**, **삭제**.
- 저장/불러오기 버튼.
- 파일 없음 → **No Recipe** 상태.

---

## 4. 티칭

- 상하 2분할: 좌 — 모터 표(이름, 포지션명, 티칭값, vel/acc/dec, 이동 버튼), 하단 **Jog**(속도+좌우 누름 유지), **IncMove**(속도+거리+Move), **Stop**(빨간 강조 스타일 `StopButton`).
- 좌측 상단/툴바: **레시피 저장/불러오기**.
- 우: 선택 축 상태(ServoOn, Home, Moving, Fault, ±Limit 등), 명명 규칙 `UpperChamber_Z`, `Motor_U` 등.

---

## 5. 매뉴얼

- 시퀀스 단위 동작 버튼(Pick/Place 등) — DO/워드와 매핑.
- **PLC I/O 패널**: DO 토글/펄스, DI 표시; **진공도·레귤레이터·ESC** 모니터링.
- **스테이지 진공 On/Off**: 피드백 일치 시 버튼 **녹색**, 연결 실패/타임아웃 **빨강**.

---

## 6. 디바이스

- PLC: IP/Port, Timeout, **시뮬 모드** 체크.
- 모션: 동일 + SPiiPlus 연결 옵션.

---

## 7. 공통 UI

- `IDialogService`: 메시지/확인/에러 — 기본 `MessageBox` 대신 스타일 통일.
- DataGrid·TabControl·ComboBox 등은 `Views/Styles/MergedThemeControls.xaml`에서 `Brush.*`를 참조한다.

---

## 8. 설비 애니메이션·스키매틱

- **범위 밖(현재)**: 메인 중앙에 2D 스키매틱·설비 애니메이션은 넣지 않는다. 3D 모니터링 탭은 기존대로 유지.

---

## 9. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안 |
| 2026-04-02 | 산업용 HMI 시각 방향·색 토큰·버튼 역할·셸 레이아웃 고정 명시; 둥근 모서리 비사용; 시퀀스 Stop 스타일 분리 |
