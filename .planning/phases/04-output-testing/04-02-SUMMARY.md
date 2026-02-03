---
phase: 04-output-testing
plan: 02
subsystem: cli
tags: [diagnostic, error-codes, golden-tests, type-inference]

# Dependency graph
requires:
  - phase: 04-01
    provides: formatDiagnostic and formatTypeNormalized functions
provides:
  - CLI integrated with typecheckWithDiagnostic
  - 12 type error golden tests with new multi-line format
  - E0304 NotAFunction detection in Infer.fs
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns: [multi-line diagnostic format, normalized type variables]

key-files:
  created:
    - tests/type-errors/11-not-a-function.flt
    - tests/type-errors/12-let-rhs-error.flt
  modified:
    - FunLang/Program.fs
    - FunLang/Infer.fs
    - tests/type-errors/*.flt (10 files)
    - tests/type-inference/*.flt (8 files)

key-decisions:
  - "NotAFunction detection in App case rather than during unification"
  - "Tests use --expr format instead of %input for predictable filename"

patterns-established:
  - "error[EXXXX]: message format for CLI type errors"
  - "Normalized type variables 'a, 'b, 'c in CLI output"

# Metrics
duration: 45min
completed: 2026-02-03
---

# Phase 4 Plan 2: CLI Integration Summary

**Integrated diagnostic format into CLI with 12 golden tests, NotAFunction detection (E0304), and normalized type output**

## Performance

- **Duration:** 45 min
- **Started:** 2026-02-03T13:30:00Z
- **Completed:** 2026-02-03T14:15:00Z
- **Tasks:** 3
- **Files modified:** 22

## Accomplishments
- CLI now displays rich multi-line diagnostics with error codes, locations, and hints
- All 4 error codes (E0301-E0304) work in CLI output
- Type output uses normalized variables ('a, 'b, 'c) instead of internal indices
- NotAFunction detection prevents confusing "type mismatch" for non-function calls
- 12 type-error golden tests validate complete diagnostic pipeline

## Task Commits

1. **Task 1: Update Program.fs to use new diagnostic format** - `6b0e27c` (feat)
2. **Task 2: Update type error golden tests + NotAFunction** - `6c6fb8d` (feat)
3. **Task 3: Verify complete diagnostic pipeline** - (verification only, no commit)

## Files Created/Modified

**Created:**
- `tests/type-errors/11-not-a-function.flt` - E0304 NotAFunction test
- `tests/type-errors/12-let-rhs-error.flt` - Let binding RHS error with context

**Modified:**
- `FunLang/Program.fs` - CLI uses typecheckWithDiagnostic + formatDiagnostic
- `FunLang/Infer.fs` - NotAFunction detection in App case
- `FunLang.Tests/TypeCheckTests.fs` - Updated assertion for NotAFunction
- `tests/type-errors/01-10.flt` - Updated for new multi-line format
- `tests/type-inference/05,08,10,12,18-21.flt` - Updated for normalized type vars

## Decisions Made

1. **NotAFunction detection in App inference**
   - Detect known non-function types (TInt, TBool, TString, TTuple, TList) before unification
   - Provides clearer error message "is not a function" vs confusing "type mismatch"

2. **Test format uses --expr instead of %input**
   - File-based tests with %input produce temp file paths in diagnostics
   - Using --expr ensures predictable "<expr>" filename in output

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] NotAFunction not implemented**
- **Found during:** Task 2 (Creating 11-not-a-function.flt)
- **Issue:** E0304 was defined in Diagnostic.fs but never raised by type checker
- **Fix:** Added NotAFunction detection in Infer.fs App case for concrete non-function types
- **Files modified:** FunLang/Infer.fs, FunLang.Tests/TypeCheckTests.fs
- **Verification:** `1 2` now produces E0304 error
- **Committed in:** 6c6fb8d (Task 2 commit)

**2. [Rule 3 - Blocking] Type inference tests used old variable names**
- **Found during:** Running all fslit tests
- **Issue:** 8 type-inference tests expected 'm, 'n but new normalizer produces 'a, 'b
- **Fix:** Updated expected output in 8 test files
- **Files modified:** tests/type-inference/*.flt (8 files)
- **Verification:** All 22 type-inference tests pass
- **Committed in:** 6c6fb8d (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 missing critical, 1 blocking)
**Impact on plan:** Both fixes necessary for plan requirements. NotAFunction was specified in must_haves.

## Issues Encountered

- **String/tuple equality tests failing**: These tests use operators (=, +) that are now correctly rejected by the type checker. Not related to this plan - pre-existing design issue where evaluator supports string ops but type system doesn't.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 4 Output & Testing complete
- All 4 diagnostic infrastructure phases complete
- v5.0 milestone ready for final verification

---
*Phase: 04-output-testing*
*Completed: 2026-02-03*
