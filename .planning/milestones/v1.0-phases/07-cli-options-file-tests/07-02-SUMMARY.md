---
phase: 07-cli-options-file-tests
plan: 02
subsystem: testing
tags: [fslit, cli-testing, regression, file-tests]

# Dependency graph
requires:
  - phase: 07-01
    provides: CLI with --emit-tokens, --emit-ast, file input support
provides:
  - Regression test suite for all CLI options (--expr, --emit-tokens, --emit-ast, file input)
  - fslit test files covering CLI-01 through CLI-05 requirements
  - Self-contained tests using %input variable for file-based tests
affects: [03-variables-binding, 04-control-flow, 05-functions-abstraction]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "fslit file-based testing with Command/Input/Output markers"
    - "%input variable for self-contained file tests"

key-files:
  created:
    - tests/cli.flt
    - tests/emit-tokens.flt
    - tests/emit-ast.flt
    - tests/file-input.flt
  modified: []

key-decisions:
  - "Verified test expectations against actual CLI output before committing"
  - "Used fslit %input variable for self-contained file tests"
  - "Organized tests by CLI option (cli, emit-tokens, emit-ast, file-input)"

patterns-established:
  - "fslit format: // --- Command:, // --- Input:, // --- Output:"
  - "Test files organized by CLI option rather than by feature"
  - "All test expectations verified against actual CLI output"

# Metrics
duration: 3min
completed: 2026-01-30
---

# Phase 07 Plan 02: CLI Options & File-Based Tests Summary

**fslit regression test suite covering --expr evaluation, --emit-tokens, --emit-ast, and file input with %input variable**

## Performance

- **Duration:** 3 min (161 seconds)
- **Started:** 2026-01-30T05:14:14Z
- **Completed:** 2026-01-30T05:17:15Z
- **Tasks:** 3
- **Files created:** 4

## Accomplishments
- Created tests/ directory with 4 fslit test files
- 21 total test cases covering all CLI options (CLI-01 through CLI-05)
- All test expectations verified against actual CLI output
- Self-contained file tests using fslit's %input variable

## Task Commits

Each task was committed atomically:

1. **Task 1: Create tests Directory and Basic CLI Tests** - `b10b577` (test)
   - tests/cli.flt with 6 test cases for --expr evaluation

2. **Task 2: Create Emit-Tokens and Emit-AST Tests** - `a93d8f8` (test)
   - tests/emit-tokens.flt with 4 test cases for --emit-tokens
   - tests/emit-ast.flt with 6 test cases for --emit-ast

3. **Task 3: Create File Input Tests with %input Variable** - `9442d04` (test)
   - tests/file-input.flt with 5 test cases for file-based execution

**Plan metadata:** (to be committed separately)

## Files Created/Modified

### Created
- `tests/cli.flt` - Basic CLI evaluation tests (6 cases: addition, precedence, parentheses, unary minus, division, complex expressions)
- `tests/emit-tokens.flt` - Token emission tests (4 cases: simple expression, all operators, parentheses, unary minus)
- `tests/emit-ast.flt` - AST emission tests (6 cases: addition, precedence, parentheses, negation, subtraction, division)
- `tests/file-input.flt` - File input tests (5 cases: basic file execution, complex expression, emit-tokens with file, emit-ast with file, multi-digit numbers)

## Test Coverage

**CLI-01:** Basic evaluation (`funlang --expr "2 + 3"` → 5)
- ✓ tests/cli.flt: 6 test cases

**CLI-02:** File execution (`funlang program.fun`)
- ✓ tests/file-input.flt: 5 test cases using %input variable

**CLI-03:** Token emission (`funlang --emit-tokens --expr "2 + 3"`)
- ✓ tests/emit-tokens.flt: 4 test cases

**CLI-04:** AST emission (`funlang --emit-ast --expr "2 + 3"`)
- ✓ tests/emit-ast.flt: 6 test cases

**CLI-05:** Combined options (file + emit)
- ✓ tests/file-input.flt: includes emit-tokens and emit-ast with file input

## Decisions Made

**1. Verified test expectations against actual CLI output**
- Ran each test case manually before writing expected output
- Ensures tests match actual behavior, not assumed behavior
- Prevents false failures due to formatting differences

**2. Used fslit %input variable for file-based tests**
- Enables self-contained tests without external file dependencies
- fslit creates temporary file with Input section content
- Cleaner than maintaining separate .fun files for each test

**3. Organized tests by CLI option**
- Separate files for cli, emit-tokens, emit-ast, file-input
- Easier to locate specific test failures
- Aligns with CLI option categories in ROADMAP.md

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for:**
- Phase 3 (Variables & Binding) - main track development
- Phase 4-6 development - testing infrastructure in place

**Test infrastructure:**
- Regression tests prevent breaking existing CLI behavior
- Easy to add new tests as features are added
- fslit provides clear pass/fail feedback

**Blockers/Concerns:**
- None

---
*Phase: 07-cli-options-file-tests*
*Completed: 2026-01-30*
