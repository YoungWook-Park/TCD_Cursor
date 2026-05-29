#  프로그램 아키텍처 및 테스트 구조

> 프로젝트의 구조 및 기능 설명 문서입니다.

## 목차

- [솔루션 구조](#솔루션-구조)
  - [Tcd.App](#tcdapp) 
  - [Tcd.Engine](#tcdengine)
  - [Tcd.Engine.Test](#tcdenginetest)
  - [Tcd.Plc.Simulator](#tcdplcsimulator)
  - [Tcd.Robot.Simulator](#tcdrobotsimulator)
  - [Tcd.Simulator](#tcdsimulator)
  - [Tcd.Simulator.Test](#tcdsimulatortest)
  - [Tcd.Tests.Shared](#tcdtestsshared)
- [의존성 방향](#의존성-방향)
- [MVVM 패턴](#mvvm-패턴)
- [테스트 케이스](#테스트-케이스)

## Tcd.App

Wpf 기반의 메인 애플리케이션 프로젝트로, UI, 디바이스 연결, 로직 실행을 담당합니다.

#### 역할
- 사용자 인터페이스 진입점 (App.xaml)
- 장비 제어 UI 제공 (Robot, PLC, Motor, Axis)
- 디바이스 통신 연결 관리.

#### 폴더 구조

| 폴더        | 설명                                      |
| --------- | --------------------------------------- |
| Core      | 레시피, 티치 포지션 데이터 모델                      |
| Define    | 알람, 로봇, PLC 상수 정의                       |
| Devices   | RobotTcpClient, PlcTcpClient            |
| Mvvm      | NotifyPropertyChangedBase, RelayCommand |
| Spii      | ACS SPiiPlus 연결/모션 서비스                  |
| Sequences | 수동 축 제어 시퀀스                             |
| View      | Equipment, Manual, Recipe, Plc 화면       |
| Styles    | WPF 컨트롤 스타일 리소스                         |

## Tcd.Engine

애플리케이션의 핵심 도메인 로직 프로젝트로 UI와 무관한 순수 비즈니스 로직, 인터페이스 계약, 공통 서비스를 담당합니다.

#### 역할
- 디바이스 추상화(인터페이스 계약 정의)
- 시퀀스 흐름 관리
- 알람 관리
- 로깅 시스템
- 자재 추적

#### 폴더 구조

| 폴더             | 설명                                 |
| -------------- | ---------------------------------- |
| Devices        | 디바이스 인터페이스 계약 (Motion, Robot, PLC) |
| Sequences      | 시퀀스 계약, 그래프, 매니저                   |
| Logging        | 로그 작성/싱크/레벨 정의                     |
| Materials      | 자재 모델 및 추적기                        |
| Alarms         | 알람 모델                              |
| AlarmManager   | 알람 상태 관리                           |
| Time / Timeout | 시간 유틸리티                            |
