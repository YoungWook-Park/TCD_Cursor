# /done — 작업 마무리

오늘 변경된 내용을 분석하고, 작업 로그를 기록하고, GitHub에 커밋·푸시한다.
`$ARGUMENTS`가 있으면 커밋 메시지 힌트로 사용한다.

---

## 실행 순서

### 1. 변경 내용 파악
아래 세 명령을 병렬로 실행해 현재 상태를 파악한다.
```
git status
git diff --stat HEAD
git log --oneline -5
```

### 2. 변경 내용 분석
변경 파일을 아래 영역으로 분류한다.
- **Engine** (`Tcd.Engine/`) — 도메인 로직, 시퀀스, 모션 계약
- **Simulator** (`Tcd.Simulator/`) — 시퀀스 레지스트리, 시뮬레이션
- **App/UI** (`Tcd.App/View/`) — ViewModel, XAML
- **Sequences** (`Tcd.App/Sequences/`) — Manual/SemiAuto/Auto 시퀀스
- **Core** (`Tcd.App/Core/`) — MainCore, 설정
- **Define** (`Tcd.App/Define/`) — 상수 클래스
- **Styles** (`Tcd.App/Styles/`) — 스타일, 리소스
- **Docs** (`docs/`) — 문서

### 3. 작업 로그 업데이트
`D:\project\TCD_Cursor\docs\WORKLOG.md` 파일의 맨 위에 아래 형식으로 오늘 항목을 추가한다.
날짜가 이미 있으면 해당 날짜 항목 아래에 이어서 작성한다.

```markdown
## YYYY-MM-DD

### 변경 내용
- [영역] 변경 사항 한 줄 요약
- [영역] 변경 사항 한 줄 요약

### 브랜치 / 커밋
- Branch: <branch명>
- Commit: <해시> — <메시지>
```

### 4. 커밋 메시지 작성
형식:
```
<type>(<scope>): <한국어 또는 영어 요약>

- 변경 항목 1
- 변경 항목 2
```
- type: `feat` `fix` `refactor` `style` `docs` `chore` `test`
- scope: `engine` `simulator` `app` `ui` `sequence` `motion` `define` `core`
- `$ARGUMENTS`가 있으면 요약 힌트로 활용

### 5. 스테이징 & 커밋
```
git add -A
git commit -m "<작성한 메시지>"
```
변경 없으면 "커밋할 내용이 없습니다"를 출력하고 중단한다.

### 6. 푸시
```
git push origin <현재 브랜치>
```
- 원격이 앞서 있으면 `git pull --rebase` 먼저 실행 후 재시도한다.
- 충돌 발생 시 사용자에게 알리고 중단한다. 절대 force push 하지 않는다.

### 7. 결과 리포트
```
✅ 완료

브랜치: <branch>
커밋:   <hash> <message>
파일:   N개 변경
로그:   docs/WORKLOG.md 업데이트 완료
```

---

## 주의
- `.env`, 시크릿, 인증 정보가 포함된 파일이 스테이지에 올라가면 즉시 경고하고 중단
- 5 MB 초과 바이너리 파일이 있으면 `.gitignore` 추가를 권고
- force push(`--force`)는 사용자가 명시적으로 요청한 경우에만 허용
