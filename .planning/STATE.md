# Project State: LangTutorial

## Current Status

**Phase:** Phase 7 - CLI Options & File-Based Tests
**Status:** ◐ In Progress (1/1 plans complete)
**Progress:** 3/7 phases complete (43%)
**Last activity:** 2026-01-30 - Completed plan 07-01

```
Phase 1 [██████████] 100% ✓ Complete
Phase 2 [██████████] 100% ✓ Complete
Phase 3 [○○○○○○○○○○] 0%   ← Next (main track)
Phase 4 [○○○○○○○○○○] 0%
Phase 5 [○○○○○○○○○○] 0%
Phase 6 [○○○○○○○○○○] 0%
Phase 7 [██████████] 100% ✓ Complete (parallel track)
```

---

## Project Reference

**See:** .planning/PROJECT.md (updated 2025-01-30)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

**Current focus:** Phase 1 준비 - .NET 10 + FsLexYacc 프로젝트 설정 및 Lexer/Parser 파이프라인 구축

**Tech stack:**
- F# (.NET 10)
- FsLexYacc (fslex + fsyacc)
- Discriminated Unions for AST
- Expecto for testing (Phase 6)

---

## Phase Status

| Phase | Name | Status | Plans | Requirements | Progress |
|-------|------|--------|-------|--------------|----------|
| 1 | Foundation & Pipeline | ● Complete | 3/3 | 4 | 100% |
| 2 | Arithmetic Expressions | ● Complete | 2/2 | 4 | 100% |
| 3 | Variables & Binding | ○ Pending | 0/0 | 3 | 0% |
| 4 | Control Flow | ○ Pending | 0/0 | 4 | 0% |
| 5 | Functions & Abstraction | ○ Pending | 0/0 | 4 | 0% |
| 6 | Quality & Polish | ○ Pending | 0/0 | 3 | 0% |
| 7 | CLI Options & File-Based Tests | ● Complete | 1/1 | 5 | 100% |

**Legend:**
- ○ Pending: Not started
- ◐ In Progress: Active work
- ● Complete: All requirements met

---

## Performance Metrics

**Velocity:** 1 phase/session (Phase 1 complete)
**Avg plans per phase:** 3.0 (3 plans in Phase 1)
**Completion rate:** 17% (1/6 phases)

**Milestones:**
- [ ] Phase 2 complete: 첫 실행 가능한 계산기
- [ ] Phase 5 complete: Turing-complete 언어 달성
- [ ] Phase 6 complete: 전체 튜토리얼 완성

---

## Accumulated Context

### Roadmap Evolution

- Phase 7 added: CLI Options & File-Based Tests (parallel with Phase 3-6)

### Decisions Made

| Decision | Phase | Rationale | Date |
|----------|-------|-----------|------|
| 7-phase structure | Roadmap | Added Phase 7 for CLI/file tests, can run parallel to main track | 2026-01-30 |
| 6-phase structure | Roadmap | Natural boundaries by language feature, aligns with research | 2025-01-30 |
| Foundation first | Phase 1 | Must establish pipeline before adding features | 2025-01-30 |
| Sequential dependencies | Roadmap | Each phase builds on previous infrastructure | 2025-01-30 |
| /tutorial command | Pre-project | Created as Claude command instead of phase | 2025-01-30 |
| Target .NET 10 | 01-01 | Latest .NET version for modern F# language features and performance | 2026-01-30 |
| FsLexYacc 11.3.0 | 01-01 | Stable version compatible with .NET 10 | 2026-01-30 |
| Minimal AST in Phase 1 | 01-01 | Number-only Expr type proves pipeline; arithmetic operators in Phase 2 | 2026-01-30 |
| Compilation order Ast.fs first | 01-01 | F# requires dependencies compiled before usage | 2026-01-30 |
| FsYacc before FsLex build order | 01-02 | Lexer.fsl opens Parser module, so Parser.fs must exist first | 2026-01-30 |
| Generated files in source directory | 01-02 | FsLexYacc default behavior (not obj/) - reference directly in .fsproj | 2026-01-30 |
| FSharp.Text.Lexing namespace | 01-02 | Required in Lexer.fsl for LexBuffer type access | 2026-01-30 |
| Track generated files in git | 01-03 | Generated Parser/Lexer files tracked for reproducible builds | 2026-01-30 |
| Build order documentation in .fsproj | 01-03 | Prevent future "Parser not found" errors with clear comments | 2026-01-30 |
| Pattern matching order in CLI | 07-01 | Most specific patterns first to avoid F# unreachable pattern warnings | 2026-01-30 |
| formatToken sprintf for NUMBER | 07-01 | Show value in token output for debugging (NUMBER(5) vs NUMBER) | 2026-01-30 |
| --emit-type reservation | 07-01 | Reserve CLI interface for future type checking phase | 2026-01-30 |

### Active TODOs

**Next action:** Plan Phase 3 (Variables & Binding) - main track development

**Blocking issues:** None

**Research gaps:**
- Phase 3: Scope management strategies (simple vs nested)
- Phase 5: Closure representation techniques (to be researched during planning)

### Known Blockers

None currently.

---

## Session Continuity

**Last session:** 2026-01-30 - Plan 07-01 execution (Phase 7 COMPLETE)
**What happened:** Completed plan 07-01 (CLI Options & File Input). Created Format.fs with token formatter and lex helper. Expanded Program.fs with --emit-tokens, --emit-ast, --emit-type options and file input support. 2 tasks, 2 commits (35c4e15, e833d17). Phase 7 complete - CLI foundation ready for testing infrastructure.
**What's next:** Plan Phase 3 (Variables & Binding) for main track, or continue with Phase 4-6
**Stopped at:** Phase 7 complete
**Resume file:** None

**If continuing from interruption:**
1. Review ROADMAP.md for phase structure
2. Check this STATE.md for current phase
3. Review REQUIREMENTS.md for requirement details
4. Proceed with `/gsd:plan-phase <number>` for next phase

**Key files:**
- `.planning/ROADMAP.md` - Phase structure and success criteria
- `.planning/REQUIREMENTS.md` - Detailed requirements with traceability
- `.planning/PROJECT.md` - Core value and constraints
- `.planning/research/SUMMARY.md` - Research findings and architecture guidance

---

## Notes

**Project characteristics:**
- Tutorial project (educational focus, not product)
- Solo developer workflow (user + Claude)
- Sequential phases (each builds on previous)
- Each phase produces working, testable code

**Critical success factors:**
1. Each chapter must run independently
2. Progressive complexity (one feature per chapter)
3. Complete working examples at each step
4. Clear documentation for F# developers

**Anti-patterns to avoid:**
- Non-incremental structure (breaking code between chapters)
- Horizontal layers (all models, then all APIs)
- Build order dependency issues (Parser must generate before Lexer)
- Insufficient error handling
- Mega interpreter anti-pattern (monolithic code)

---

*Last updated: 2026-01-30*
*Next update: After Phase 2 planning*
