# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-03)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.
**Current focus:** v6.0 Bidirectional Type System

## Current Position

**Milestone:** v6.0 Bidirectional Type System
**Phase:** Phase 6 - Migration (in progress)
**Plan:** 1 of 02 complete (06-02)
**Status:** Plan 06-02 complete, plan 06-01 pending
**Last activity:** 2026-02-04 - Completed 06-02-PLAN.md (Chapter 12 tutorial)

Progress: █████████████████████████░░░ 5/6 phases (83%)
Phase 1: Complete (3/3 plans, 7/7 requirements)
Phase 2: Complete (2/2 plans, 3/3 requirements)
Phase 3: Complete (2/2 plans, 7/7 requirements)
Phase 4: Complete (2/2 plans, 4/4 requirements)
Phase 5: Complete (1/1 plans, 3/3 requirements)
Phase 6: In progress (1/2 plans, 1/3 requirements)

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
| Subsumption as fallback | Check mode falls back to synthesis + unification for flexible typing (03-01) |
| Reuse Infer module functions | freshVar, instantiate, generalize from Infer for consistency (03-01) |
| Build order: Bidir after Infer | Bidir depends on Infer helpers (freshVar, instantiate, generalize, inferPattern) (03-02) |
| Backward compatibility testing | Compare Bidir types with Algorithm W to ensure no regressions (03-02) |
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
| TypeCheck uses Bidir.synthTop | Required for annotation type checking to work in CLI and integration tests (04-02) |
| Type erasure in Eval | Annot/LambdaAnnot evaluate inner expression; runtime ignores type annotations (04-02) |
| InCheckMode stores (Type, source, Span) | Flexible source tracking for annotation, if-branch, etc. error contexts (05-01) |
| Annotation hint replaces generic hint | When annotation context present, hint explains where expected type came from (05-01) |
| Tutorial Korean/English mixed style | Follow established pattern from chapters 10-11 (06-02) |

### Pending Todos

None.

### Blockers/Concerns

- Pre-existing BidirTests parse errors (11 tests) due to syntax mismatches - not blocking progress

## Session Continuity

Last session: 2026-02-04
Stopped at: Completed 06-02-PLAN.md (Chapter 12 tutorial)
Resume file: None
Next: Run 06-01-PLAN.md to complete Phase 6
