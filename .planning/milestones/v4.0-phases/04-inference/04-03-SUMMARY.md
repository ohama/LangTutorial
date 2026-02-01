---
phase: 04-inference
plan: 03
subsystem: type-inference
tags: [hindley-milner, lambda, application, let-polymorphism, letrec, algorithm-w]

# Dependency graph
requires:
  - phase: 04-02
    provides: infer function structure with literals, operators, variables
provides:
  - Lambda inference with fresh parameter type
  - Application inference with unification
  - Let with let-polymorphism (generalize-instantiate)
  - LetRec with pre-bound recursive reference
affects: [04-05, integration tests, REPL type display]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Lambda: fresh var for param, monomorphic binding in body"
    - "App: fresh result type, unify func with arg->result arrow"
    - "Let: generalize AFTER infer, apply subst to env before generalize"
    - "LetRec: pre-bind function name for recursive calls"

key-files:
  created: []
  modified:
    - FunLang/Infer.fs

key-decisions:
  - "Lambda params monomorphic (no polymorphism inside lambda bodies)"
  - "Let-polymorphism via generalize-instantiate pattern at let boundaries"
  - "LetRec generalizes for polymorphic recursion"

patterns-established:
  - "Substitution threading: applyEnv before next inference"
  - "Pre-binding pattern for recursive definitions"

# Metrics
duration: 1min
completed: 2026-02-01
---

# Phase 04 Plan 03: Lambda/App/Let/LetRec Inference Summary

**Core binding forms implemented: Lambda creates arrow types, App unifies, Let enables polymorphism, LetRec enables recursion**

## Performance

- **Duration:** 1 min
- **Started:** 2026-02-01T11:00:24Z
- **Completed:** 2026-02-01T11:01:24Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments

- Lambda infers TArrow with fresh parameter type, applies substitution correctly
- Application creates fresh result type and unifies function with arg->result arrow
- Let implements let-polymorphism via generalize-instantiate pattern
- LetRec pre-binds function name for recursive body inference

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Lambda and App inference** - `10a2378` (feat)
2. **Task 2: Add Let and LetRec inference** - `8b33791` (feat)

## Files Created/Modified

- `FunLang/Infer.fs` - Added Lambda, App, Let, LetRec cases to infer function

## Decisions Made

- **Lambda params monomorphic:** Parameters bound with empty scheme `Scheme([], paramTy)` - no polymorphism within lambda bodies, consistent with HM
- **Let-polymorphism:** Generalize AFTER inferring value type, apply substitution to env before generalize for correct free variable calculation
- **LetRec pre-binding:** Create fresh funcTy and paramTy, bind name before body inference, unify with actual inferred arrow type

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Core binding forms complete
- Ready for If expression (04-04) and Tuple/List (04-05) inference
- Let-polymorphism enables polymorphic functions like `let id = \x -> x in ...`

---
*Phase: 04-inference*
*Completed: 2026-02-01*
