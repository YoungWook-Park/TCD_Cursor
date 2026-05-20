# SPEC: CSV 로그

현재 구현: `Tcd.Engine/Logging/LogWriter.cs` (비동기 배치, 4096 용량, 100개/500ms)

---

## 경로

- 로그 폴더: `%TEMP%\Tcd\Logs\` (현재 구현)
- 일별 파일: `tcd_yyyyMMdd.csv`

> 확장 계획: `{RecipeRoot}\Logs\` 경로로 변경 (레시피 폴더 하위)

---

## CSV 형식

헤더: `Timestamp,Level,BlockName,Message`

- **Timestamp**: 로컬 시각 (ISO 8601)
- **Level**: `Debug`, `Info`, `Warn`, `Error`
- **BlockName**: 시퀀스 키, 함수명 등 (`LogContext.SequenceKey`)
- **Message**: 내용 (RFC 4180 이스케이프)

---

## 보관 정책 (계획)

- 기본 14일 보관 (`LogRetentionDays`)
- 정리 시점: 앱 시작 또는 1일 1회 타이머
- 기준: 파일명 날짜 < `UtcNow - RetentionDays`

---

## UI

- 메인 화면 **Logs** 버튼 → 파일 목록 + 내용 표시 창
- 메인 UI에 실시간 로그 스트림 바인딩 없음
