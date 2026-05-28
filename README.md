## 🏢 프로젝트 개요

- PLC와 Spiiplus SDK를 이용한 필름 합착 시뮬레이터, 실 장비 대상으로 기 구현된 솔루션을 재구성하였습니다. 솔루션의 제어로직 및 레시피들을 직접 관리하면서 0 to 100까지의 개발을 수행합니다.

## 🏔️ 목적

- 비동기 제어 등 플랫폼 고정부의 구조 및 심층 학습으로 개발 역량 강화
- 기존 레거시 코드 단순 구현으로 프로젝트 리펙토링을 통한 아키텍처 적인 고민으로 더 효율적이고 클린한 코드를 구현
- 단위 테스트/통합 테스트를 테스트 코드 및 케이스를 작성하면서 솔루션 품질 향상

## 🧿 프로젝트를 수행하는 이유

- L사 플랫폼의 API 제공으로 로직 단순 구현으로 프로젝트 개발 시 한계
- 장비 제어 프로젝트의 공통 기능 내재화 및 자체 프로젝트로 설비 프로젝트에 재활용
- 프로그램 설계서, 이슈 관리, 테스트 문서를 문서화 함으로써 체계적인 프로젝트 관리를 학습

## ⚒️ 환경

| 항목 | 내용 |
| --- | --- |
| 언어 / 프레임워크 | C# / .NET 8 / WPF |
| IDE | VS Studio 2026 / Cursor (AI-assisted) |
| 테스트 도구 | xUnit  |
| AI Agent | Claude Code v2.1.152 |
| SDK | Spiiplus MMI Application Studio 3.13.01 |

## 🚗 빌드 및 테스트 명령

```jsx
 # 전체 빌드
  dotnet build TCD_Corsur.sln

  # 로봇 시뮬레이터 실행
  dotnet run --project Tcd.Robot.Simulator/Tcd.Robot.Simulator.csproj

  # PLC 시뮬레이터 실행
  dotnet run --project Tcd.Plc.Simulator/Tcd.Plc.Simulator.csproj
  
  # 앱 실행
  dotnet run --project Tcd.App/Tcd.App.csproj

  # 단위 테스트
  dotnet test Tcd.Engine.Tests/Tcd.Engine.Tests.csproj
  dotnet test Tcd.Simulator.Tests/Tcd.Simulator.Tests.csproj
```

## 🏢 아키텍처

기술 분석 문서

AI 코드 역설계 기반 아키텍처 분석 시리즈.

| # | 항목 | 상태 |
|---|------|------|
| 1 | [UI 화면 구성 — 레시피·세팅·디바이스](docs/analysis/01_ui_components.md) | 작성 중 |
| 2 | [프로그램 아키텍처 및 테스트 구조](docs/analysis/02_architecture_and_tests.md) | 예정 |
| 3 | [시퀀스 제어 구조](docs/analysis/03_sequence_control.md) | 예정 |
| 4 | [하드웨어 구성](docs/analysis/04_hardware.md) | 예정 |
