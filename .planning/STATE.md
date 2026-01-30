# Project State: LangTutorial

## Current Status

**Phase:** Phase 3 - Variables & Binding
**Status:** ● Complete (2/2 plans complete)
**Progress:** 5/7 phases complete (71%)
**Last activity:** 2026-01-30 - Completed plan 03-02

```
Phase 1 [██████████] 100% ✓ Complete
Phase 2 [██████████] 100% ✓ Complete
Phase 3 [██████████] 100% ✓ Complete
Phase 4 [○○○○○○○○○○] 0%   ← Next (main track)
Phase 5 [○○○○○○○○○○] 0%
Phase 6 [○○○○○○○○○○] 0%
Phase 7 [██████████] 100% ✓ Complete (parallel track)
```

---

## Project Reference

**See:** .planning/PROJECT.md (updated 2025-01-30)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

**Current focus:** Phase 4 준비 - Control Flow (if-then-else, 조건 평가)

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
| 3 | Variables & Binding | ● Complete | 2/2 | 3 | 100% |
| 4 | Control Flow | ○ Pending | 0/0 | 4 | 0% |
| 5 | Functions & Abstraction | ○ Pending | 0/0 | 4 | 0% |
| 6 | Quality & Polish | ○ Pending | 0/0 | 3 | 0% |
| 7 | CLI Options & File-Based Tests | ● Complete | 2/2 | 5 | 100% |

**Legend:**
- ○ Pending: Not started
- ◐ In Progress: Active work
- ● Complete: All requirements met

---

## Performance Metrics

**Velocity:** 2 phases/session
**Avg plans per phase:** 2.0 (10 plans in 5 phases)
**Completion rate:** 71% (5/7 phases)

**Milestones:**
- [x] Phase 2 complete: 첫 실행 가능한 계산기
- [ ] Phase 5 complete: Turing-complete 언어 달성
- [ ] Phase 6 complete: 전체 튜토리얼 완성
- [x] Phase 7 complete: CLI 옵션 및 파일 기반 테스트

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
| Verify tests against actual output | 07-02 | All test expectations verified by running CLI before committing | 2026-01-30 |
| fslit %input variable for files | 07-02 | Self-contained tests without external file dependencies | 2026-01-30 |
| Organize tests by CLI option | 07-02 | Separate files for cli, emit-tokens, emit-ast, file-input | 2026-01-30 |
| Environment as Map<string, int> | 03-01 | O(log n) lookup, immutable, functional style for variable storage | 2026-01-30 |
| evalExpr wrapper function | 03-01 | Hides environment plumbing from Program.fs for top-level calls | 2026-01-30 |
| Lexer keyword ordering | 03-01 | Keywords (let, in) before identifier pattern to prevent IDENT match | 2026-01-30 |
| failwithf for undefined vars | 03-01 | Simple error handling now, to be enhanced with proper types in Phase 6 | 2026-01-30 |
| One test per file in fslit | 03-02 | Organize tests in category directories (tests/variables/, etc.) | 2026-01-30 |
| Numbered test files | 03-02 | Clear ordering and readability (01-basic-let.flt, etc.) | 2026-01-30 |

### Active TODOs

**Next action:** Plan Phase 4 (Control Flow) - main track development

**Blocking issues:** None

**Research gaps:**
- Phase 5: Closure representation techniques (to be researched during planning)

### Known Blockers

None currently.

---

## Session Continuity

**Last session:** 2026-01-30 - Plan 03-02 execution (Phase 3 COMPLETE)
**What happened:** Completed plan 03-02 (Variable Binding Tests). Created 12 fslit tests for all variable binding requirements (VAR-01, VAR-02, VAR-03). Tests cover let binding, variable references, local scope, shadowing, and token/AST output. Added variables target to Makefile. All 33 tests pass (12 variables + 21 existing). 2 tasks, 2 commits (4d3d512, cd3f434). Phase 3 fully complete with comprehensive test coverage.
**What's next:** Plan Phase 4 (Control Flow) for main track
**Stopped at:** Completed 03-02-PLAN.md
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
*Next update: After Phase 4 planning*
