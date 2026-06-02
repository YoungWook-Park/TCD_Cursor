# GitHub Issue Workflow

이 프로젝트는 **이슈 단위로 브랜치를 생성하고 PR로 머지**하는 워크플로우를 사용합니다.
`github-issue-flow` 에이전트가 브랜치 생성부터 PR 오픈까지 자동으로 처리합니다.

---

## 전제 조건

- `gh` CLI 설치 및 로그인 완료
  ```bash
  gh auth login
  ```
- 작업 시작 전 `main` 브랜치가 최신 상태여야 합니다
  ```bash
  git checkout main && git pull
  ```

---

## 전체 흐름

```
GitHub Issue (#N)
      ↓
  브랜치 생성        ← 에이전트 START 단계
feature/issue-N-slug
      ↓
   코드 작업
      ↓
  커밋 + 푸시        ← 에이전트 DONE 단계
      ↓
   PR 생성
(Closes #N 자동 링크)
      ↓
  리뷰 & 머지
      ↓
  이슈 자동 닫힘
```

---

## 사용법

### 1단계 — 작업 시작 (브랜치 생성)

이슈 번호를 언급하며 시작 의사를 표현하면 에이전트가 자동으로 트리거됩니다.

```
이슈 #5 시작할게. 브랜치 따줘
```

```
issue #12 작업 시작해줘
```

에이전트가 수행하는 작업:
1. `gh issue view N`으로 이슈 제목 확인
2. 브랜치명 생성: `feature/issue-N-short-slug`
3. `git checkout -b feature/issue-N-slug`
4. 결과 리포트

출력 예시:
```
✅ 이슈 브랜치 준비 완료

🔖 이슈: #5 — AutoRunSequence 전체 사이클 구현
🌿 브랜치: feature/issue-5-auto-run-sequence

이제 작업을 시작하세요. 완료되면 "이슈 #5 완료. PR 올려줘"라고 말씀해주세요.
```

---

### 2단계 — 코드 작업

평소처럼 코드를 수정합니다. 브랜치는 이미 체크아웃된 상태입니다.

```bash
# 현재 브랜치 확인
git branch --show-current
# → feature/issue-5-auto-run-sequence
```

---

### 3단계 — 작업 완료 (커밋 + 푸시 + PR)

완료 의사를 표현하면 에이전트가 나머지를 처리합니다.

```
이슈 #5 구현 완료. PR 올려줘
```

```
이슈 #5 완료
```

에이전트가 수행하는 작업:
1. 변경사항 분석 및 커밋 메시지 작성
2. `git add -A && git commit` (커밋에 `Closes #5` 포함)
3. `git push -u origin feature/issue-5-auto-run-sequence`
4. `gh pr create --base main` 으로 PR 생성
5. PR URL 리포트

출력 예시:
```
✅ PR 생성 완료

🔖 이슈: #5 — AutoRunSequence 전체 사이클 구현
🌿 브랜치: feature/issue-5-auto-run-sequence
📝 커밋: a1b2c3d — feat(sequence): AutoRunSequence 전체 사이클 구현
🔗 PR: https://github.com/YoungWook-Park/TCD_Cursor/pull/7

PR이 main에 머지되면 이슈 #5이 자동으로 닫힙니다.
```

---

## 브랜치 네이밍 규칙

| 이슈 유형 | 브랜치 패턴 | 예시 |
|---|---|---|
| 기능 구현 | `feature/issue-N-slug` | `feature/issue-5-auto-run-sequence` |
| 버그 수정 | `feature/issue-N-slug` | `feature/issue-12-sim-axis-thread-safety` |
| 리팩토링 | `feature/issue-N-slug` | `feature/issue-9-alarm-keys-refactor` |

슬러그 규칙: 이슈 제목을 영어 소문자 + 하이픈으로 변환, 최대 40자.

---

## 에이전트 역할 분리

| 에이전트 | 트리거 | 역할 |
|---|---|---|
| `github-issue-flow` | "이슈 #N 시작/완료" | 이슈 단위 브랜치 → PR |
| `work-commit-pusher` | "오늘 작업 마무리" | 이슈 없는 일반 세션 커밋/푸시 |

---

## 주의사항

- **Force push 금지** — 에이전트는 절대 `--force`를 사용하지 않습니다. 충돌 시 사용자에게 보고합니다.
- **하드코딩 감지** — `Tcd.App/Define/` 외부에 문자열 리터럴이 있으면 커밋 전에 경고합니다.
- **하나의 이슈 = 하나의 브랜치 = 하나의 PR** — 여러 이슈를 하나의 PR로 묶지 않습니다.
- **`gh` 미인증 상태** — 에이전트가 `gh auth login` 실행을 안내합니다 (직접 실행은 불가).
