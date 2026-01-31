---
phase: 02-strings
plan: 01
subsystem: language-core
tags: [fslex, fsyacc, string-literals, escape-sequences, type-checking]

# Dependency graph
requires:
  - phase: 01-comments
    provides: Comment handling in lexer (pattern order)
provides:
  - String data type with full pipeline integration
  - Escape sequence support (\n, \t, \\, \")
  - String concatenation and comparison operators
  - Type-safe operations with clear error messages
affects: [03-repl, future phases using string operations]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - State machine pattern for string lexing (read_string)
    - Escape sequence handling in lexer
    - Type-safe operator overloading (Add for int+int and string+string)

key-files:
  created:
    - tests/strings/ (15 fslit tests)
  modified:
    - FunLang/Ast.fs (String expr, StringValue)
    - FunLang/Lexer.fsl (read_string state machine)
    - FunLang/Parser.fsy (STRING token)
    - FunLang/Eval.fs (string evaluation, extended operators)
    - FunLang/Format.fs (STRING token formatting)
    - FunLang.Tests/Program.fs (29 new tests)

key-decisions:
  - "Lexer handles escape sequences (not parser) for clean separation"
  - "Heredoc approach for Lexer.fsl to avoid shell escaping issues with fslex patterns"
  - "Type error messages specify both operand types for clarity"

patterns-established:
  - "State machine pattern: read_string buf = parse for stateful lexing"
  - "Escape before literal: \\n rule must come before generic character rule"
  - "Error precedence: specific errors (newline, EOF) before catch-all"

# Metrics
duration: 11min
completed: 2026-01-31
---

# Phase 02 Plan 01: Strings Summary

**String data type with literals, escape sequences (\n, \t, \\, \"), concatenation (+), comparison (=, <>), and comprehensive error handling**

## Performance

- **Duration:** 11 minutes
- **Started:** 2026-01-31T14:26:07Z
- **Completed:** 2026-01-31T14:37:38Z
- **Tasks:** 3
- **Files modified:** 8

## Accomplishments
- Full string type support from lexer through evaluation
- Escape sequence handling in lexer state machine
- Type-safe string concatenation and comparison
- 44 new tests (15 fslit + 29 Expecto) covering all 12 requirements

## Task Commits

Each task was committed atomically:

1. **Task 1: Add String AST, Lexer, and Parser support** - `4a9a92a` (feat)
   - String/StringValue in AST
   - STRING token in Parser
   - read_string state machine with escape sequences

2. **Task 2: Add String evaluation and formatting** - `aa4c61b` (feat)
   - String literal evaluation
   - Extended Add for string concatenation
   - Extended Equal/NotEqual for string comparison
   - formatValue and formatToken for strings

3. **Task 3: Add comprehensive tests** - `dcbb6d4` (test)
   - 15 fslit tests in tests/strings/
   - 29 Expecto tests covering STR-01 to STR-12
   - All 93 fslit tests pass (regression clean)
   - All 168 Expecto tests pass

## Files Created/Modified

**Created:**
- `tests/strings/01-simple-string.flt` - Basic string literal test
- `tests/strings/02-empty-string.flt` - Empty string test
- `tests/strings/03-escape-newline.flt` - \n escape test
- `tests/strings/04-escape-tab.flt` - \t escape test
- `tests/strings/05-escape-backslash.flt` - \\ escape test
- `tests/strings/06-escape-quote.flt` - \" escape test
- `tests/strings/07-concat-strings.flt` - Concatenation test
- `tests/strings/08-concat-empty.flt` - Concat with empty test
- `tests/strings/09-equal-true.flt` - String equality test
- `tests/strings/10-equal-false.flt` - String inequality test
- `tests/strings/11-notequal-true.flt` - Not-equal true test
- `tests/strings/12-notequal-false.flt` - Not-equal false test
- `tests/strings/13-string-in-let.flt` - Let binding integration
- `tests/strings/14-string-in-if.flt` - If expression integration
- `tests/strings/15-unterminated-error.flt` - Error handling test

**Modified:**
- `FunLang/Ast.fs` - Added String expr and StringValue variant
- `FunLang/Lexer.fsl` - Added read_string state machine
- `FunLang/Parser.fsy` - Added STRING token and Atom rule
- `FunLang/Eval.fs` - String evaluation and extended operators
- `FunLang/Format.fs` - STRING token formatting
- `tests/Makefile` - Added strings target
- `FunLang.Tests/Program.fs` - Added stringTests section with 29 tests
- Added evaluateToString helper function

## Decisions Made

**1. Heredoc approach for Lexer.fsl**
- **Context:** Initial attempts to write escape patterns directly in Lexer.fsl failed with "unterminated string in code" errors from fslex
- **Decision:** Used bash heredoc to write entire Lexer.fsl file, avoiding shell escaping issues
- **Rationale:** fslex parser is sensitive to escape sequences in patterns; heredoc ensures exact content delivery
- **Impact:** Successful build, all tests pass

**2. Pattern order in read_string state machine**
- **Decision:** Escape sequences ("\\n", "\\t") before generic character matcher
- **Rationale:** Lexer matches first rule that succeeds; escapes must be checked before wildcard
- **Pattern established:** Specific before general (applies to all fslex rules)

**3. Type error message format**
- **Decision:** "Type error: + requires operands of same type (int or string)"
- **Rationale:** Explicitly states supported types for better developer experience
- **Alternative considered:** Generic "type mismatch" - rejected as less helpful

**4. Error handling for unterminated strings**
- **Decision:** Two error cases: "Newline in string literal" (when newline encountered) and "Unterminated string literal" (when EOF encountered)
- **Rationale:** More specific error messages help developers identify the exact issue
- **Testing note:** fslit %input doesn't include trailing newline, so EOF case is more common

## Deviations from Plan

**1. [Deviation - Build Issue] Lexer.fsl escape sequence syntax**
- **Found during:** Task 1 (Lexer implementation)
- **Issue:** Initial escape patterns using `'\\' 'n'` syntax caused fslex "parse error"
- **Investigation:** Tried multiple pattern syntaxes, all failed with "unterminated string in code"
- **Fix:** Rewrote entire Lexer.fsl using heredoc with string patterns ("\\n" instead of '\\' 'n')
- **Files modified:** FunLang/Lexer.fsl
- **Verification:** Build succeeds, all escape sequence tests pass
- **Impact:** No functional change, only syntax adjustment for fslex compatibility

**2. [Deviation - Test Adjustment] Unterminated string error message**
- **Found during:** Task 3 (fslit test execution)
- **Issue:** Test expected "Newline in string literal" but got "Unterminated string literal"
- **Root cause:** fslit %input substitution doesn't include trailing newline, so lexer hits EOF before newline
- **Fix:** Updated test expectation to match actual behavior
- **Files modified:** tests/strings/15-unterminated-error.flt
- **Verification:** Test now passes, error handling still correct for both cases
- **Impact:** Test matches implementation reality

---

**Total deviations:** 2 (1 build workaround, 1 test adjustment)
**Impact on plan:** Both deviations necessary for correctness. No scope creep. All 12 requirements (STR-01 to STR-12) implemented as specified.

## Issues Encountered

**fslex escape sequence parsing**
- **Problem:** fslex failed to parse escape sequence patterns in multiple syntaxes ('\\' 'n', "\\n" initially, etc.)
- **Solution:** Used heredoc to ensure clean file content without shell interference
- **Learning:** fslex is sensitive to escape sequences in pattern definitions; heredoc provides clean delivery

**Test framework behavior**
- **Problem:** Understanding how fslit handles %input (doesn't include trailing newline)
- **Solution:** Adjusted test expectation to match actual behavior
- **Learning:** fslit %input is different from file input for error cases

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for Phase 03 (REPL):**
- String type fully integrated in evaluator
- Type errors properly reported
- All existing functionality (arithmetic, booleans, functions) still works
- Test coverage comprehensive (93 fslit + 168 Expecto)

**No blockers.** String support complete and tested.

**Technical foundation:**
- State machine pattern established for future complex lexing
- Type-safe operator extension pattern proven
- Error message conventions established

---
*Phase: 02-strings*
*Completed: 2026-01-31*
