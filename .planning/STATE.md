# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-03)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.
**Current focus:** v6.0 Bidirectional Type System

## Current Position

**Milestone:** v6.0 Bidirectional Type System
**Phase:** 01-parser-extensions (Phase 1 of 6)
**Plan:** 01-01 complete, 01-02 next
**Status:** In progress
**Last activity:** 2026-02-03 — Completed 01-01-PLAN.md

Progress: ██░░░░░░░░░░░░░░░░░░░░░░░░░░ 1/3 plans (33%)
Phase 1: ██░░░░░░░░ 1/3 plans complete

## Milestone Summary

**v6.0 Bidirectional Type System** in progress:
- 6 phases, 27 requirements
- Complete transition from Algorithm W to bidirectional
- ML-style type annotations: `fun (x: int) -> x + 1`
- Curried multi-parameter: `fun (x: int) (y: int) -> e`
- Polymorphic annotations: `let id (x: 'a) : 'a = x`

See: .planning/ROADMAP.md for phase details

## Accumulated Context

### Decisions

| Decision | Rationale |
|----------|-----------|
| ML-style annotations | Familiar syntax, matches existing lambda syntax |
| Curried multi-parameter | `fun (x: int) (y: int) -> e` matches FunLang's curried style |
| Hybrid approach | Fresh vars for unannotated lambdas preserves backward compatibility |
| Keep let-polymorphism | Orthogonal to bidirectional structure, maintains expressiveness |
| Type var includes apostrophe | TYPE_VAR lexeme captures full `'a` string for simpler parser handling (01-01) |
| COLON after CONS | Ensures "::" lexes as single token, not two colons (01-01) |
| TypeExpr without Span | Type expressions don't cause runtime errors; Span kept on enclosing expression (01-01) |

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-03
Stopped at: Completed 01-01-PLAN.md
Resume file: None
Next: Execute 01-02-PLAN.md (Parser grammar rules)
