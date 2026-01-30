---
phase: 01-comments
plan: 01
subsystem: lexer
tags: [comments, fslex, lexer]

# Dependency graph
requires:
  - phase: v1.0-mvp
    provides: Core lexer/parser/eval infrastructure
provides:
  - Single-line comment support (//)
  - Multi-line block comment support (* *)
  - Nested comment handling with depth tracking
  - Unterminated comment error detection
affects: [02-strings, 03-repl]

# Tech tracking
tech-stack:
  added: []
  patterns: [Recursive lexer rules with state (depth parameter)]

key-files:
  created:
    - tests/comments/ (12 fslit test files)
  modified:
    - FunLang/Lexer.fsl
    - tests/Makefile
    - FunLang.Tests/Program.fs

key-decisions:
  - "Single-line comments use // syntax (C-style) not # (shell-style)"
  - "Block comments use (* *) syntax (ML-family) not /* */ (C-style)"
  - "Comments handled in lexer, not parser - tokens never generated"
  - "Pattern order critical: comment patterns before operator patterns"

patterns-established:
  - "Comment rules use 'and' keyword to define separate lexer rules with parameters"
  - "Block comments track nesting depth via recursive rule parameter"
  - "Error handling via failwith in lexer rules"

# Metrics
duration: 8min
completed: 2026-01-31
---

# Phase 1 Plan 01: Comments Summary

**Single-line (//) and nested block comment (* *) support added to FunLang lexer with 22 new tests**

## Performance

- **Duration:** 8 minutes
- **Started:** 2026-01-30T23:35:51Z
- **Completed:** 2026-01-30T23:44:16Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- Single-line comments (//) skip to end of line without generating tokens
- Block comments (* *) support multi-line and arbitrary nesting depth
- Unterminated block comments produce clear error messages
- All existing functionality preserved (division, parentheses still work)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add comment rules to Lexer.fsl** - `d4bbe85` (feat)
2. **Task 2: Create comment tests directory and fslit files** - `ec1c6ea` (test)
3. **Task 3: Add Expecto unit tests for comment lexer** - `cbc7251` (test)

## Files Created/Modified
- `FunLang/Lexer.fsl` - Added comment lexer rules (single-line and block_comment)
- `tests/comments/` - 12 new fslit test files covering all comment scenarios
- `tests/Makefile` - Added comments target
- `FunLang.Tests/Program.fs` - Added 10 Expecto tests for comment functionality

## Decisions Made

**Pattern Order:**
- Comment patterns (`//` and `(*`) placed BEFORE operator patterns (`/` and `(`) in lexer rules
- This ensures comments are recognized before operators, preventing incorrect tokenization

**Comment Style Choice:**
- Single-line: `//` (C-style) chosen for familiarity
- Block: `(* *)` (ML-style) chosen for F# consistency and nesting support
- Alternative `/* */` rejected: doesn't support nesting in most languages

**Error Handling:**
- Unterminated comments detected at EOF with `failwith "Unterminated comment"`
- Error message includes "Unterminated comment" for user clarity

**Implementation Strategy:**
- Comments handled entirely in lexer via recursive consumption
- No AST nodes generated - comments are truly invisible to parser

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

**fslit Error Test Format:**
- **Issue:** Initial test 11-unterminated-error.flt failed because fslit wasn't capturing stderr
- **Resolution:** Added `2>&1` to command to redirect stderr to stdout, changed expected output from "Unterminated comment" to "Error: Unterminated comment" to match actual output format
- **Files modified:** tests/comments/11-unterminated-error.flt
- **Impact:** Minor test format correction, no code changes required

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Comment support complete and tested
- Ready for Phase 2 (Strings) - no blockers
- Ready for Phase 3 (REPL) - comment parsing will work in interactive mode
- All 78 fslit tests passing (66 existing + 12 new)
- All 139 Expecto tests passing (129 existing + 10 new)

## Test Coverage

**fslit Tests (12 new):**
- Single-line comments: basic, only-line, mid-expression
- Block comments: simple, multiline, mid-expression
- Nested comments: simple, deep (3 levels)
- Mixed comments: both styles in same code
- Operator preservation: division, parentheses still work
- Error handling: unterminated comment detection
- Let expressions: comments in variable bindings

**Expecto Tests (10 new):**
- CMT-01: Single-line comments (2 tests)
- CMT-02: Block comments (3 tests)
- CMT-03: Nested comments (2 tests)
- CMT-04: Error handling (1 test)
- Non-interference (2 tests)

## Technical Notes

**Lexer Rule Implementation:**

```fsl
// Single-line: match until newline
| "//" [^ '\n' '\r']*  { tokenize lexbuf }

// Block comment: recursive rule with depth parameter
| "(*"  { block_comment 1 lexbuf }

and block_comment depth = parse
    | "(*"    { block_comment (depth + 1) lexbuf }  // Nest deeper
    | "*)"    { if depth = 1 then tokenize lexbuf   // Exit
                else block_comment (depth - 1) lexbuf }
    | newline { block_comment depth lexbuf }
    | eof     { failwith "Unterminated comment" }
    | _       { block_comment depth lexbuf }
```

**Key Insight:** The `block_comment` rule is parameterized by `depth` to track nesting level. When depth reaches 1 and `*)` is encountered, control returns to main `tokenize` rule. This enables ML-style nested comments where `(* outer (* inner *) outer *)` correctly pairs delimiters.

---
*Phase: 01-comments*
*Completed: 2026-01-31*
