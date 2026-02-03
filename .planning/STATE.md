# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-03)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.
**Current focus:** v6.0 Bidirectional Type System

## Current Position

**Milestone:** v6.0 Bidirectional Type System
**Phase:** Phase 2 in progress (Type Expression Elaboration)
**Plan:** 02-02 complete
**Status:** In progress
**Last activity:** 2026-02-03 — Completed 02-02-PLAN.md (Elaborate Module Tests)

Progress: █████████░░░░░░░░░░░░░░░░░░░ 2/6 phases (33%)
Phase 1: ✓ Complete (3/3 plans, 7/7 requirements)
Phase 2: ▶ In progress (2/3 plans)

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
| Grammar structure for precedence | Three-level hierarchy (Arrow > Tuple > Atomic) avoids shift/reduce conflicts (01-02) |
| Right-associative arrow via recursion | ArrowType rule recursive at same level makes int -> int -> int = int -> (int -> int) (01-02) |
| Annotated expression rule ordering | Placed AFTER plain parens but BEFORE tuple to disambiguate via COLON token (01-03) |
| Dual annotated lambda rules | Single-parameter and curried rules coexist; fsyacc prefers more specific (01-03) |
| Curried parameter desugaring | fun (x: T) (y: U) -> e desugars to nested LambdaAnnot nodes via desugarAnnotParams (01-03) |
| Separate type var index ranges | User type vars (elaboration) start at 0, inference type vars start at 1000+ to avoid collision (02-01) |
| Environment threading in elaboration | Thread Map<string, int> through elaboration to ensure 'a in same scope maps to same TVar index (02-01) |
| Two elaboration APIs | elaborateTypeExpr (fresh scope) vs elaborateScoped (shared scope for curried params) (02-01) |
| Test type vars by pattern matching | Test TVar indices by matching structure, not exact values (indices are implementation detail) (02-02) |

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-03 11:03:33 UTC
Stopped at: Completed 02-02-PLAN.md
Resume file: None
Next: Plan 02-03 (Bidirectional Type Checker Integration)
