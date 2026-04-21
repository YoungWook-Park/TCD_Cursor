# SPEC: CSV 로그·보관

| 항목 | 내용 |
|------|------|
| 관련 PRD | [PRD_Lamination_Simulator.md](PRD_Lamination_Simulator.md) |

---

## 1. 경로

- 기본 레시피 루트: `D:\VCD_Recipe` (설정으로 오버라이드).
- 로그 폴더: **`{RecipeRoot}\Logs`** (예: `D:\VCD_Recipe\Logs`).
- 일별 파일: `yyyy-MM-dd.csv` 또는 `log_yyyyMMdd.csv`.

---

## 2. CSV 형식

헤더:

`Timestamp,Level,BlockName,Message`

- **Timestamp**: UTC 권장(ISO 8601) 또는 로컬(문서·구현 일치).
- **Level**: `Critical`, `Error`, `Warning`, `Information`, …
- **BlockName**: 함수명, 시퀀스 스텝 ID, `nameof` 등.
- **Message**: 예외 메시지·설명. CSV 이스케이프(RFC 4180).

---

## 3. 보관 정책

- 설정 키: `LogRetentionDays` (기본 14).
- **상한 14일** 클램프(요구사항).
- 정리 시점: 앱 시작, 로그 기록 후(선택), 일 1회 타이머.
- 삭제 기준: 파일 **이름 날짜** 또는 **LastWriteTime** < `UtcNow - RetentionDays`.

---

## 4. UI

- **메인 화면 로그 버튼** → 별도 창/페이지에서 파일 목록 + 내용 표시.
- 메인 UI에 **실시간 로그 스트림 바인딩 없음**(요구사항).

---

## 5. 변경 이력

| 일자 | 내용 |
|------|------|
| 2026-03-31 | 초안 |
