# TCD_Cursor

WPF 기반 **Thermal Compression Bonding** 장비 HMI 시뮬레이터. 포트폴리오 프로젝트.

제조 설비 소프트웨어의 HMI · 시퀀스 엔진 · 모션 제어 · 로봇 통신 · 로깅을 end-to-end로 구현.

---

## 빌드 및 실행

```bash
# 전체 빌드
dotnet build TCD_Corsur/TCD_Corsur.sln

# 앱 실행
dotnet run --project TCD_Corsur/Tcd.App/Tcd.App.csproj

# 로봇 시뮬레이터 (별도 터미널)
dotnet run --project TCD_Corsur/Tcd.Robot.Simulator/Tcd.Robot.Simulator.csproj
```

> ACS SPiiPlus 하드웨어 SDK(`Dll/ACS.SPiiPlusNET.dll`)가 없으면 `Tcd.App`은 빌드 경고가 발생하지만 시뮬레이터 모드(`UseSpiiPlus=false`)로 정상 실행됩니다.

---

## 솔루션 구조

```
Tcd.App              — WPF UI + 컴포지션 루트 (net8.0-windows)
Tcd.Engine           — 도메인 순수 라이브러리 (netstandard2.0)
Tcd.Simulator        — 인프로세스 디바이스 에뮬레이터 (netstandard2.0)
Tcd.Robot.Simulator  — 독립 TCP 로봇 시뮬레이터 서버
```

의존 방향: `Tcd.App` → `Tcd.Engine` ← `Tcd.Simulator`

---

## 주요 기능

| 기능 | 설명 |
|------|------|
| **자동 사이클** | 스테이지 로드 → 상·하부 필름 로드 → UVW 정렬 → 본딩 → 언로드 |
| **반자동 시퀀스** | 5개 단계별 독립 실행 |
| **수동 제어** | U/V/W/ZLower/ZUpper 5축 × 8가지 조작 |
| **레시피 관리** | 축 티칭·속도·로봇 위치 JSON 저장 |
| **로봇 TCP 통신** | Line-delimited JSON, Heartbeat 300ms |
| **SPiiPlus 연동** | ACS 하드웨어 or 인프로세스 시뮬레이터 전환 |
| **비동기 로깅** | CSV 배치 기록, `%TEMP%\Tcd\Logs\` |

---

## 문서

| 파일 | 내용 |
|------|------|
| [TCD_Corsur/CLAUDE.md](TCD_Corsur/CLAUDE.md) | 코드 구조·아키텍처·키 파일 맵 |
| [docs/SPEC_Sequence.md](docs/SPEC_Sequence.md) | 시퀀스 계층 및 AUTO 흐름 |
| [docs/SPEC_Robot_Interface.md](docs/SPEC_Robot_Interface.md) | 로봇 TCP 프로토콜 |
| [docs/SPEC_Motion_SpiiPlus.md](docs/SPEC_Motion_SpiiPlus.md) | ACS SPiiPlus 변수·버퍼 계약 |
| [docs/SPEC_Layout_Recipe.md](docs/SPEC_Layout_Recipe.md) | 설비 레이아웃·레시피 데이터 모델 |
| [docs/SPEC_Control_IO.md](docs/SPEC_Control_IO.md) | PLC I/O 맵 (확장 계획) |
| [docs/SPEC_UI_Hmi.md](docs/SPEC_UI_Hmi.md) | HMI 화면 구성 |
| [docs/ROADMAP.md](docs/ROADMAP.md) | 다음 작업 순서 |
