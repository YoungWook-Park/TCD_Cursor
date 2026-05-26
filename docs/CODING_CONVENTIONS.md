# Coding Conventions

직접 작성·관리하는 팀 코딩 규칙. 구현 전 여기를 먼저 확인한다.

---

## ViewModel

### 커맨드 바인딩 — 익명 함수 금지
`RelayCommand`의 실행 본문은 반드시 명명된 메서드로 분리한다.

```csharp
// Bad
public ICommand Cmd_Save => cmd_Save ??= new RelayCommand(_ => { ... });

// Good
public ICommand Cmd_Save => cmd_Save ??= new RelayCommand(OnCmd_Save);
private void OnCmd_Save(object? _) { ... }
```

### 프로퍼티 setter — 두 가지 패턴만 허용
부수 효과가 없으면 단순 Set, 있으면 명명된 핸들러 호출.

```csharp
// 부수 효과 없음 — 한 줄
public string Status { get => _status; private set => Set(ref _status, value); }

// 부수 효과 있음 — 핸들러 호출
public string? SelectedName
{
    get => _selectedName;
    set { if (!Set(ref _selectedName, value)) return; OnSelectedNameChanged(); }
}
private void OnSelectedNameChanged() { ... }
```

### 생성자 인수 — 한 줄로
생성자가 한 줄에 들어오면 줄 바꿈 없이 작성한다.

```csharp
// Bad
new RobotVelocityEditRow(
    RobotPositionName.Home,
    RobotVelocityDefault.Home)

// Good
new RobotVelocityEditRow(RobotPositionName.Home, RobotVelocityDefault.Home),
```

---

## Recipe 접근

레시피 저장·불러오기는 반드시 `MainCore.SaveRecipe` / `MainCore.LoadRecipe`를 통한다.
`IRecipeRepository`를 ViewModel에서 직접 호출하지 않는다.

```csharp
// Bad — Repository 직접 접근, RecipeStore 갱신 누락 위험
_core.RecipeRepository.Save(r);
_core.Recipes.Current = r;

// Good — 두 작업이 항상 함께 일어남이 보장됨
_core.SaveRecipe(r);
```

---

## 추가 규칙 (여기서부터 직접 작성)

