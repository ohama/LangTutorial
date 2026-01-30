---
phase: 07-cli-options-file-tests
plan: 01
subsystem: cli
tags: [fslex, fsyacc, cli, f#, pattern-matching, file-io]

# Dependency graph
requires:
  - phase: 02-arithmetic-expressions
    provides: Parser tokens (NUMBER, PLUS, etc.), AST types, Eval module
provides:
  - Format.fs module with token formatting and lex helper
  - CLI support for --emit-tokens, --emit-ast, --emit-type options
  - File input support (funlang program.fun)
  - Pattern-matched CLI argument handling
affects: [future phases needing intermediate representation inspection, testing infrastructure]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Array pattern matching for CLI args ordered from most specific to least specific"
    - "File.Exists guard clauses in pattern matching"
    - "Token list accumulation with tail recursion"

key-files:
  created:
    - FunLang/Format.fs
  modified:
    - FunLang/Program.fs
    - FunLang/FunLang.fsproj

key-decisions:
  - "formatToken uses sprintf for NUMBER to show value, plain strings for operators"
  - "lex helper accumulates tokens in reverse, then reverses for efficiency"
  - "Pattern order: specific multi-arg patterns before single-arg filename patterns"
  - "--emit-type reserved for future type checking phase"

patterns-established:
  - "Emit options follow pattern: --emit-X --expr <expr> or --emit-X <filename>"
  - "formatTokens uses String.concat with space separator"
  - "AST printing uses F# %A formatter for automatic pretty-printing"

# Metrics
duration: 3min
completed: 2026-01-30
---

# Phase 7 Plan 01: CLI Options & File Input Summary

**CLI with --emit-tokens/--emit-ast inspection, file input support, and Format module for token/AST display**

## Performance

- **Duration:** 3 min
- **Started:** 2026-01-30T05:08:21Z
- **Completed:** 2026-01-30T05:10:53Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Users can inspect tokens with `--emit-tokens` for debugging lexer
- Users can inspect AST with `--emit-ast` for understanding parser output
- Programs can be loaded from .fun files instead of only --expr
- Format module provides reusable token formatting and lexing utilities

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Format.fs with Token Formatter and Lex Helper** - `35c4e15` (feat)
   - Created Format.fs module
   - Implemented formatToken with pattern matching on Parser.token types
   - Implemented formatTokens using List.map and String.concat
   - Implemented lex helper using LexBuffer and tail recursion
   - Updated FunLang.fsproj to add Format.fs after Lexer, before Eval

2. **Task 2: Expand Program.fs with Full CLI Options** - `e833d17` (feat)
   - Added System.IO for File.ReadAllText and File.Exists
   - Opened Format module for lex and formatTokens
   - Implemented 11 pattern-matched CLI scenarios
   - Ordered patterns from most specific to least specific
   - Added --emit-tokens for token display (expr and file)
   - Added --emit-ast for AST display (expr and file)
   - Added --emit-type as reserved (returns error message)
   - Added file input support with File.Exists guards
   - Updated help text with all new options

## Files Created/Modified
- `FunLang/Format.fs` - Token formatting and lex helper module
- `FunLang/Program.fs` - Expanded CLI with emit options and file input
- `FunLang/FunLang.fsproj` - Added Format.fs to build order

## Decisions Made

1. **formatToken output format**: Use `sprintf "NUMBER(%d)" n` for NUMBER tokens to show the value, plain strings like "PLUS" for operators. Makes token streams readable while preserving detail.

2. **lex accumulation strategy**: Accumulate tokens in reverse during recursion, then reverse at end. Standard F# pattern for efficient list building.

3. **Pattern matching order**: Most specific patterns (multi-argument) before less specific (single argument). Prevents F# compiler warning about unreachable patterns. Help patterns before single-arg filename patterns.

4. **--emit-type reservation**: Return error message "Type checking not yet implemented. Reserved for future phase." Establishes CLI interface before implementing type checking.

5. **AST display format**: Use F#'s `%A` formatter for AST - automatic pretty-printing of discriminated unions without custom formatting code.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

**Pattern order warning (resolved)**: Initial pattern ordering placed `| [| "--help" |]` after `| [| filename |]`, causing FS0026 warning "This rule will never be matched". Resolved by moving help patterns before single-argument filename patterns. F# matches patterns top-to-bottom, so more specific patterns must come first.

## Next Phase Readiness

- CLI foundation complete for testing infrastructure
- Format.fs provides token/AST utilities for test assertions
- File input enables batch testing with .fun files
- Ready for Phase 7 Plan 02 (if any) or parallel Phase 3-6 development

**Blockers:** None

---
*Phase: 07-cli-options-file-tests*
*Completed: 2026-01-30*
