---
phase: 04-output-testing
plan: 01
subsystem: diagnostics
tags: [formatting, type-normalization, rust-style-errors]
dependency-graph:
  requires: [03-01]
  provides: [formatTypeNormalized, formatDiagnostic]
  affects: [04-02]
tech-stack:
  added: []
  patterns: [normalized-type-variables, rust-inspired-diagnostics]
key-files:
  created: []
  modified: [FunLang/Type.fs, FunLang/Diagnostic.fs, FunLang.Tests/TypeTests.fs]
decisions:
  - "Normalized variables use first-appearance order for consistent 'a, 'b, 'c naming"
  - "Diagnostic format follows Rust-style: error[CODE]: message -> location = context"
metrics:
  duration: ~16 minutes
  completed: 2026-02-03
---

# Phase 4 Plan 01: Formatting Functions Summary

Implemented formatting functions for user-friendly diagnostic output with normalized type variables and Rust-style multi-line error format.

## What Was Built

### formatTypeNormalized in Type.fs
- Collects type variables in depth-first order of first appearance
- Maps any TVar index to sequential 'a, 'b, 'c names
- TVar 1000, TVar 1001 -> 'a, 'b (consistent normalization)
- Handles complex types: arrows (with parentheses), tuples, lists

### formatDiagnostic in Diagnostic.fs
- Rust-inspired multi-line error format:
```
error[E0301]: Type mismatch: expected int but got bool
 --> test.fun:3:10-14
   = in if condition: test.fun:3:4-20
   = note: in if then-branch at test.fun:3:4
   = hint: Check that all branches of your expression return the same type
```
- Error code header (optional)
- Primary span location
- Secondary spans with labels
- Notes from context stack and trace
- Hint for suggested fix

### Unit Tests (9 new tests)
- 6 tests for formatTypeNormalized
  - Single variable normalization
  - Two variables to 'a -> 'b
  - Variable reuse ('a -> 'b -> 'a)
  - Complex polymorphic type (map signature)
  - Tuple with variables
  - Concrete types unchanged
- 3 tests for formatDiagnostic
  - Full format with all fields
  - Format without error code
  - Multiple notes

## Technical Details

### formatTypeNormalized Algorithm
1. Traverse type depth-first, collecting TVar indices in order
2. Build Map from original index to sequential 0, 1, 2...
3. Format type using mapped indices as 'a, 'b, 'c

### formatDiagnostic Structure
1. Error header: `error[CODE]: message` or `error: message`
2. Primary location: ` --> file:line:col`
3. Secondary spans: `   = label: location`
4. Notes: `   = note: text`
5. Hint: `   = hint: text`

## Commits

| Commit | Type | Description |
|--------|------|-------------|
| 2ae7ec5 | feat | Add formatTypeNormalized to Type.fs |
| 3645c5d | feat | Add formatDiagnostic to Diagnostic.fs |
| 3d77a57 | test | Add unit tests for formatting functions |

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- [x] `dotnet build FunLang` compiles without errors
- [x] `dotnet run --project FunLang.Tests` passes all tests (378 total)
- [x] formatTypeNormalized normalizes TVar 1000+ to 'a, 'b, 'c
- [x] formatDiagnostic produces multi-line Rust-style output

## Test Count

- Before: 369 Expecto tests
- After: 378 Expecto tests (+9)

## Next Phase Readiness

Ready for 04-02 (Integration with TypeCheck). The formatting functions are complete and tested. TypeCheck.typecheck can now call typeErrorToDiagnostic and formatDiagnostic to produce user-friendly error messages.
