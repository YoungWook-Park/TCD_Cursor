# Roadmap

현재 구현 완료 상태와 다음 작업 순서.

---

## 현재 완료

- WPF HMI (메인 화면, 매뉴얼, 레시피 탭)
- 시퀀스 엔진: Atomic → Manual → SemiAuto → Auto 4계층
- IMotionService 추상화 (SPiiPlus 하드웨어 / 인프로세스 시뮬레이터 전환)
- 로봇 TCP 시뮬레이터 서버 (Line-delimited JSON, Heartbeat)
- 레시피 JSON 저장/불러오기
- 비동기 CSV 로그 (배치 기록, %TEMP%\Tcd\Logs\)
- 인프로세스 자재 추적 (Pick/Place 시뮬레이션)

---

## 다음 작업 (우선순위 순)

### 1단계 — UI 개선

- 알람 리스트와 시퀀스 트레이스 로그 분리 (현재 혼용)
- MainWindowViewModel: 현재 레시피·축 상태 표시 개선
- 장비 개략도 애니메이션 (ZUpper↓ / ZLower↑ 본딩 표시)

### 2단계 — 품질

- SequenceManager 단위 테스트
- ManualMotorViewModel 상태 갱신 테스트
- MainCore 의존성 최종 정리 (`LogContext` 제거)

### 3단계 — 기능 확장

- PLC TCP 시뮬레이터 (Bit/Word 맵 교환, [SPEC_Control_IO.md](SPEC_Control_IO.md))
- 인터락 서비스 (`InterlockService`, [SPEC_Interlocks.md](SPEC_Interlocks.md))
- CSV 14일 보관 정책 ([SPEC_Logging_Csv.md](SPEC_Logging_Csv.md))
- 합착 2초 공정 데이터 MongoDB 저장

### 4단계 — 분석 (장기)

- Python + React 로컬 대시보드 (공정 이력·트렌드)
- Helix Toolkit 3D 설비 뷰 (선택)
