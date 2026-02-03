---
phase: 01-span-infrastructure
plan: 01
subsystem: compiler-infrastructure
tags: [fslexyacc, position-tracking, source-location, error-diagnostics]

# Dependency graph
requires:
  - phase: none
    provides: Base lexer and parser infrastructure
provides:
  - Span type for source location tracking
  - Position tracking in lexer via setInitialPos and NextLine
  - Foundation for precise error messages
affects: [02-ast-spans, 03-parser-spans, 04-type-error-spans]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Span type pattern: 1-based line/column indexing matching FsLexYacc Position records"
    - "unknownSpan sentinel for built-in/synthetic AST nodes"
    - "Position tracking via lexbuf.EndPos.NextLine on all newline occurrences"

key-files:
  created: []
  modified:
    - FunLang/Ast.fs
    - FunLang/Lexer.fsl

key-decisions:
  - "Use 1-based line/column indexing to match FsLexYacc Position API"
  - "Use NextLine property instead of deprecated AsNewLinePos()"
  - "Track position in all three newline contexts: main tokenize, block comments, line comments"

patterns-established:
  - "Span type with FileName, StartLine, StartColumn, EndLine, EndColumn fields"
  - "mkSpan helper converts FsLexYacc Position records to Span"
  - "unknownSpan constant for built-in/synthetic locations (like F# compiler's range0)"
  - "formatSpan produces readable location strings for error messages"

# Metrics
duration: 10min
completed: 2026-02-02
---

# Phase 01 Plan 01: Span Infrastructure Summary

**Span type and lexer position tracking foundation for precise type error diagnostics**

## Performance

- **Duration:** 10 min
- **Started:** 2026-02-02T08:35:56Z
- **Completed:** 2026-02-02T08:46:08Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Span type with file, line, column location tracking
- Helper functions: mkSpan, unknownSpan, formatSpan for location management
- Lexer position tracking via setInitialPos and NextLine on all newlines
- Foundation ready for parser and type inference location propagation

## Task Commits

Each task was committed atomically:

1. **Task 1: Define Span type in Ast.fs** - `9838b67` (feat)
2. **Task 2: Enable lexer position tracking** - `e41a524` (feat)

## Files Created/Modified
- `FunLang/Ast.fs` - Added Span type, mkSpan, unknownSpan, formatSpan functions at module top
- `FunLang/Lexer.fsl` - Added setInitialPos function, position tracking on all newlines (tokenize, block_comment, line comment rules)

## Decisions Made

**1. Use NextLine property instead of AsNewLinePos()**
- Rationale: AsNewLinePos() is deprecated, NextLine is the modern API

**2. Track position in all three newline contexts**
- Rationale: Comments can span multiple lines, so newlines in both block and line comments must update position for accurate error location tracking

**3. Use 1-based indexing for line and column**
- Rationale: Matches FsLexYacc Position.Line and Position.Column convention, consistent with F# compiler error messages

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

**Initial build errors with Position record initialization**
- Problem: Position record required pos_orig_lnum field (not documented in plan)
- Solution: Added pos_orig_lnum: 1 to setInitialPos function
- Problem: Assignment returns unit, causing type error in comment rule
- Solution: Added semicolon before tokenize lexbuf call to sequence operations

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for Phase 2 (AST Spans):**
- Span type available for AST node annotation
- Lexer position tracking active and verified
- Helper functions available for span creation and formatting

**No blockers:**
- All tests pass (362 Expecto, verified subset of fslit)
- Position tracking verified in comments, newlines, multi-line content

---
*Phase: 01-span-infrastructure*
*Completed: 2026-02-02*
