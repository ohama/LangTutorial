---
phase: 05-error-integration
plan: 01
subsystem: diagnostics
tags: [type-errors, bidirectional, annotations, diagnostics]

# Dependency graph
requires:
  - phase: 04-annotation-checking
    provides: Annot and LambdaAnnot AST nodes, Bidir type checking
provides:
  - InCheckMode context in InferContext for annotation-aware errors
  - findExpectedTypeSource helper for extracting annotation source
  - Annotation-aware hints in type error messages
affects: [06-polish]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Mode-aware context pushing in bidirectional type checker"
    - "Context-driven error hint customization"

key-files:
  created: []
  modified:
    - FunLang/Diagnostic.fs
    - FunLang/Bidir.fs
    - tests/type-errors/13-annot-mismatch.flt
    - tests/type-errors/14-annot-mismatch-lambda.flt
    - tests/type-errors/15-lambda-annot-wrong-body.flt

key-decisions:
  - "InCheckMode stores (expected: Type, source: string, Span) for flexible source tracking"
  - "Annotation hint replaces generic branch mismatch hint when annotation context present"

patterns-established:
  - "Push InCheckMode context before entering check mode from annotation"
  - "findExpectedTypeSource searches context stack for annotation source"

# Metrics
duration: 12min
completed: 2026-02-04
---

# Phase 5 Plan 01: Mode-aware Error Diagnostics Summary

**InCheckMode context in Diagnostic.fs enabling annotation-aware type error hints that explain where expected types came from**

## Performance

- **Duration:** 12 min
- **Started:** 2026-02-04T11:00:00Z
- **Completed:** 2026-02-04T11:12:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Added InCheckMode to InferContext with expected type, source string, and span
- Bidir.fs pushes InCheckMode when entering check mode from Annot and LambdaAnnot
- Type mismatch errors now show "due to annotation" secondary span and helpful hints
- Non-annotation errors remain unchanged (backward compatible)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add InCheckMode to Diagnostic.fs with annotation-aware formatting** - `221305c` (feat)
2. **Task 2: Push InCheckMode in Bidir.fs and update golden tests** - `4d03b31` (feat)

## Files Created/Modified
- `FunLang/Diagnostic.fs` - Added InCheckMode to InferContext, findExpectedTypeSource helper, annotation-aware hints
- `FunLang/Bidir.fs` - Push InCheckMode context in Annot and LambdaAnnot cases
- `tests/type-errors/13-annot-mismatch.flt` - Updated expected output with annotation hints
- `tests/type-errors/14-annot-mismatch-lambda.flt` - Updated expected output with annotation hints
- `tests/type-errors/15-lambda-annot-wrong-body.flt` - Updated expected output with annotation hints

## Decisions Made
- InCheckMode stores `(expected: Type, source: string, Span)` to allow flexible source tracking (annotation, if-branch, etc.)
- When annotation source found, hint changes from generic "Check that all branches..." to specific "The type annotation at X expects Y"

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Error integration Phase 1 complete
- Ready for Phase 2 (multi-span error context) or other error enhancement plans
- All 419 Expecto tests pass
- All annotation-related fslit tests pass (tests 13-15 in type-errors)

---
*Phase: 05-error-integration*
*Completed: 2026-02-04*
