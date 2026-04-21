# Project Development Guidelines

Common rules for this repository (.NET / WPF and related modules).  
**Frontend React conventions are out of scope** and not included here.

---

## Code Quality

- Treat **readability** as the highest priority.
- **Reduce duplication**; extract reusable functions or classes.
- Each function and method should follow the **Single Responsibility
  Principle**.
- Use **clear, meaningful** names for variables, methods, and types.
- Replace **magic values** with named constants or configuration.

---

## Code Style

- Keep **consistent conventions** across the entire codebase.
- Use **2 spaces** for indentation.
- Prefer **lines under 80 characters** when practical (especially in code).
- Follow **standard naming** for the language in use (e.g. C# guidelines).
- Comments should explain **why** the code is written that way, not only
  **what** it does.

---

## Documentation

- Add **documentation comments** for all **public APIs** (e.g. XML `///` on
  public types and members in C#).
- Describe **setup, build, and run** steps clearly in the **README**.
- Put **complex algorithms or domain rules** in dedicated docs (e.g.
  `docs/SPEC_*.md`).
- Record notable changes in a **CHANGELOG**.

---

## Testing

- Add **unit tests** for new behavior.
- Aim for **at least 80%** code coverage where meaningful for the module.
- Tests must be **independent** and **repeatable**.
- Add **integration tests** for critical workflows (e.g. PLC map exchange,
  sequence preflight) when applicable.

---

## Security

- **Validate and sanitize** external and user input at boundaries.
- Keep **secrets** out of source control; use environment variables or
  secure storage.
- **Review dependencies** regularly for known vulnerabilities.
- Guard against common issues relevant to the stack (e.g. injection in
  queries, unsafe deserialization).

---

## Performance

- **Optimize** data access (e.g. database queries) where they matter.
- Avoid **redundant** network or IO calls.
- Use **pagination** or chunking for large result sets.
- Run **heavy work** asynchronously so UI and critical paths stay responsive.

---

## Version Control

- Write **clear, descriptive** commit messages.
- Use **feature branches** for work in progress.
- Perform **code review** before merging to the main line.
- Keep the **main branch** in a **deployable** (buildable, runnable) state.

---

## Project Structure

- Organize code with a **layered architecture** (e.g. UI, application,
  domain, infrastructure).
- Each **module or package** should have a **single, clear responsibility**.
- **Do not introduce circular dependencies** between projects or layers.
- Apply **Domain-Driven Design** only where the domain complexity justifies it.

---

## WPF UI (HMI)

- Prefer **Fluent** theme packages for a consistent dark shell (see
  `SPEC_UI_Hmi.md`).
- Keep **shared brushes, typography, and spacing** in `App.xaml` resource
  dictionaries; feature views use merged dictionaries where helpful.
- **WindowStyle=None** with custom chrome is planned for the operator shell;
  document deviations in the UI SPEC.

---

## Change History

| Date | Summary |
|------|---------|
| 2026-03-31 | WPF UI: Fluent + XAML resource layout note (ROADMAP Phase 0) |
| 2026-03-31 | English guidelines; React sections removed |
| 2026-03-31 | Prior Korean draft superseded |
