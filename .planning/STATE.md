# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-02)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.
**Current focus:** v5.0 타입 에러 진단 - Phase 1 (Span Infrastructure)

## Current Position

**Milestone:** v5.0 타입 에러 진단 — Algorithm W 에러 위치/원인 정확히 표현
**Phase:** 1 of 4 (Span Infrastructure)
**Plan:** 02 of 4 in phase (just completed)
**Status:** In progress
**Last activity:** 2026-02-02 — Completed 01-02-PLAN.md

Progress: [███░░░░░░░] 30% (2/4 plans in Phase 1 complete)

## Milestone Summary

**v4.0 타입 시스템** shipped 2026-02-01:
- 6 phases, 12 plans, 33 requirements
- Hindley-Milner type inference with Algorithm W
- 460 total tests (362 Expecto + 98 fslit)
- 4,805 lines F#

**v5.0 타입 에러 진단** started 2026-02-02:
- 4 phases, 27 requirements
- Goal: Precise diagnostics with location tracking, context awareness, helpful messages

See: .planning/MILESTONES.md for full history

## Accumulated Context

### Decisions

| Phase | Decision | Rationale | Impact |
|-------|----------|-----------|--------|
| 01-01 | Use NextLine property instead of AsNewLinePos() | AsNewLinePos() deprecated, NextLine is modern API | Cleaner code, no deprecation warnings |
| 01-01 | Track position in all three newline contexts | Comments can span multiple lines, need accurate tracking | Complete position accuracy for error messages |
| 01-01 | Use 1-based indexing for line/column | Matches FsLexYacc Position API convention | Consistent with F# compiler error format |
| 01-02 | Span as LAST named parameter | F# DU convention, enables pattern matching with _ | Clean pattern matching in all consumers |
| 01-02 | Use parseState.InputStartPosition/InputEndPosition | FsYacc standard API for position tracking | Accurate span from parser rules |

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-02 09:17
Stopped at: Completed 01-02-PLAN.md (AST Span Integration)
Resume file: None
Next: 01-03-PLAN.md (Error Formatter) or 01-04-PLAN.md (Integration Tests)
