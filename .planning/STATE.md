# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-02)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.
**Current focus:** v5.0 타입 에러 진단 - Phase 2 (Error Representation)

## Current Position

**Milestone:** v5.0 타입 에러 진단 — Algorithm W 에러 위치/원인 정확히 표현
**Phase:** 2 of 4 (Error Representation) — IN PROGRESS
**Plan:** 02 of 03 in phase (completed)
**Status:** TypeException integrated into Unify/Infer, ready for Blame Assignment
**Last activity:** 2026-02-03 — Completed 02-02-PLAN.md

Progress: [██████░░░░] 67% (2/3 plans in Phase 2 complete)

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
| 02-01 | SecondarySpans initialized empty | Phase 3 (Blame Assignment) will populate with related expression locations | typeErrorToDiagnostic returns empty SecondarySpans list |
| 02-01 | Error codes E0301-E0304 | Unique codes for UnifyMismatch, OccursCheck, UnboundVar, NotAFunction | Users can reference specific error types in documentation |
| 02-01 | Context stack and trace stored inner-first | Natural for pushing during inference, reversed for display | formatContextStack and formatTrace reverse before formatting |
| 02-02 | unifyWithContext threads context and trace | Build UnifyPath as descending into type structure for structural failure location | AtFunctionParam, AtFunctionReturn, AtTupleIndex, AtListElement track position within types |
| 02-02 | inferWithContext maintains context stack | Push InferContext before recursing for all expression types requiring recursion | Provides inference path showing where in code type checking occurred |
| 02-02 | Backward-compatible wrapper functions | unify and infer call new functions with empty context | Existing code continues to work without changes |
| 02-02 | TypeCheck dual API | typecheck (string-based) and typecheckWithDiagnostic (Diagnostic-based) | Backward compatibility + rich error access for future phases |

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-03 10:09
Stopped at: Completed 02-02-PLAN.md (Unify Integration)
Resume file: None
Next: Plan 02-03 (Blame Assignment) - populate SecondarySpans with related expression locations
