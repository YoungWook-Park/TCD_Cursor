# SPEC: ACS SPiiPlus 연동

코드 위치: `Tcd.App/Spii/` · `Tcd.App/Core/Define.cs (SpiiDefine)`

---

## 변수 계약

| 방향 | 변수 | 용도 |
|------|------|------|
| PC → ACS | `PC_ACS_DISTANCE(i)`, `VELOCITY(i)`, `ACC(i)`, `DEC(i)`, `JERK(i)` | 이동 프로파일 (전역 REAL 배열, 인덱스 = 축 번호) |
| PC → ACS | `RD_Ena_CMD`, `RD_Abs_CMD`, `RD_Inc_CMD`, `RD_pJog_CMD`, `RD_nJog_CMD`, `RD_Home_CMD`, `RD_Halt_CMD`, `RD_Fcle_CMD` | 축별 INT 배열 명령 비트 |
| ACS → PC | `ACS_PC_CURRENT_POS_AXIS{n}`, `ACS_PC_IS_MOVE/FAULT/HOME_AXIS{n}` | 모니터링 변수 |
| 공유 | `ON_MONITORING_FLAG` | 버퍼9 루프 제어 |

축 인덱스: U=0, V=1, W=2, ZLower=3, ZUpper=4

---

## 명령 흐름

### AbsMove
1. Servo ON (`RD_Ena_CMD`)
2. 레시피에서 속도/가감속/저크 로드 → 전역 배열 Write
3. 목표 거리 Write → `RD_Abs_CMD=1` (Pulse)
4. `IsMoving`/`IsFault` 폴링으로 완료 대기

### Jog (hold-to-jog)
- 버튼 누름: `RD_pJog_CMD(i)=1` / 버튼 뗌: `=0`
- 취소/종료 시 `finally`에서 조그 플래그 0 클리어
- 즉시 정지 필요 시: MMI `Halt(axis)` API 사용

### 기타
- **IncMove**: 캐시에서 현재 위치 읽어 `current + delta`로 AbsMove 호출
- **Stop**: `RD_Halt_CMD(axis)=1`
- **Home**: `RD_Ena_CMD` + `RD_Home_CMD=1` → `IsHome` 폴링
- **FaultClear**: `RD_Fcle_CMD(axis)=1`

---

## 전역 배열 Write 규칙

- PC가 쓰는 전역 배열은 **배열 전체**를 `WriteVariable(..., ACSC_NONE, -1×4)`로 전송
- 드라이버 내부에 섀도 배열(`MotionPcGlobalMirror`) 유지 — 한 축만 바꿔도 전체 배열 전송
- 펄스 명령 후: 미러의 해당 칸을 0으로 복원 (다음 전송에 1이 남지 않도록)

---

## 버퍼 9 (모니터링)

```
while(ON_MONITORING_FLAG) {
    FPOS / MFLAGS / MST / FAULT → ACS_PC_* 변수에 매핑
}
```

- **Activate**: `ON_MONITORING_FLAG=1` → 버퍼 실행
- **Deactivate**: `ON_MONITORING_FLAG=0` → 루프 종료 → 연결 해제

---

## JobQueue (PC 측)

- 단일 백그라운드 워커에서 SPiiPlus API 호출 (스레드 안전)
- 각 Job의 CancellationToken에 디바이스 설정 타임아웃 적용

---

## Stub 모드

ACS DLL 미설치 환경: `SimMotionService`로 대체 (`AppSettings.UseSpiiPlus=false`)  
UI·시퀀스 로직은 동일하게 검증 가능.
