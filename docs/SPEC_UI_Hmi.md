# SPEC: WPF HMI 화면 구성

---

## 셸(메인 창)

- `WindowStyle="None"`, `WindowChrome` 커스텀 제목줄
- 레이아웃: 상단 바 40px · 중앙 콘텐츠 `*` · 하단 내비 56px
- 상단 오른쪽: **Start auto** | **Stop** | **Logs** | 최소화 | 종료
- 하단 내비: **Device** | **Recipe** | **Teach** | **Manual** | **3D**
- `ContentControl` + `DataTemplate`로 탭 전환 (MVVM)

---

## 시각 방향 (산업용 HMI 다크)

- 남색 계열 베이스
- 가동 상태: **녹색**, 시퀀스 정지: **무채/어두운 바**, 위험 동작: **빨강**
- 버튼 모서리: 둥근 모서리 사용 안 함 (직각 유지)

### 버튼 스타일

| 스타일 | 용도 |
|--------|------|
| 기본 | 일반 동작·내비 |
| `AccentButton` | Start auto, Connect 등 주요 긍정 |
| `SequenceStopButton` | 시퀀스 Stop |
| `StopButton` | 모터 Stop, 창 종료 등 위험·강한 중단 |

---

## 화면별 구성

### 레시피
- 경로: JSON 파일, 기본 루트 설정 가능
- 좌: 레시피 목록 / 우: `TabControl` 세부 항목
- 컨텍스트 메뉴: 적용 · 복사 · 삭제
- 파일 없음 → `No Recipe` 상태

### 티칭
- 모터 표: 이름 · 포지션명 · 티칭값 · vel/acc/dec · 이동 버튼
- Jog (속도 + 방향, 누름 유지) / IncMove (속도 + 거리) / **Stop** (빨간 강조)
- 우: 선택 축 상태 (ServoOn, Home, Moving, Fault, ±Limit)
- 레시피 저장/불러오기

### 매뉴얼
- DO 토글/펄스, DI 표시
- 진공도 · 레귤레이터 · ESC 모니터링
- 스테이지 진공 On/Off: 피드백 일치 시 **녹색**, 실패 시 **빨강**

### 디바이스
- PLC: IP/Port · Timeout · 시뮬 모드 체크
- 모션: SPiiPlus IP · 연결 옵션

---

## 공통 UI

- `IDialogService`: 메시지/확인/에러 대화상자 (스타일 통일)
- 공유 브러시·타이포: `App.xaml` 리소스 딕셔너리
- View/ViewModel 쌍: 동일 기능 폴더에 위치, 별도 `ViewModels/` 루트 없음
