---
phase: 03-repl
plan: 01
subsystem: cli
tags: [argu, cli-parsing, command-line]

# Dependency graph
requires:
  - phase: 02-strings
    provides: Complete language pipeline (lexer, parser, eval)
provides:
  - Argu-based declarative CLI argument parsing
  - Auto-generated help text
  - Short flag aliases (-e for --expr)
  - REPL flag placeholder for next plan
affects: [03-02, future CLI extensions]

# Tech tracking
tech-stack:
  added: [Argu 6.2.5]
  patterns:
    - Declarative CLI with IArgParserTemplate
    - ProcessExiter for colorized error messages

key-files:
  created:
    - FunLang/Cli.fs (Argu argument type)
  modified:
    - FunLang/FunLang.fsproj (Argu package, Cli.fs compile order)
    - FunLang/Program.fs (Argu-based parsing)

key-decisions:
  - "Argu auto-converts Emit_Tokens to --emit-tokens (underscore to hyphen)"
  - "MainCommand attribute on File allows positional filename argument"
  - "raiseOnUsage = false to handle --help gracefully"

patterns-established:
  - "CLI args as discriminated union with metadata attributes"
  - "ArgumentParser.Create with programName for usage text"
  - "ProcessExiter for colorized error handling"

# Metrics
duration: 7min
completed: 2026-01-31
---

# Phase 3 Plan 1: CLI Modernization Summary

**Argu-based declarative CLI replacing 120 lines of pattern matching with auto-generated help and alias support**

## Performance

- **Duration:** 7 min
- **Started:** 2026-01-31T15:44:38Z
- **Completed:** 2026-01-31T15:51:53Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Replaced manual CLI pattern matching with Argu declarative approach
- Auto-generated help text from IArgParserTemplate usage descriptions
- Short flag aliases working (-e for --expr)
- REPL flag infrastructure ready for implementation

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Argu package and create Cli.fs module** - `188cc56` (feat)
2. **Task 2: Refactor Program.fs to use Argu** - `3eb6737` (refactor)

## Files Created/Modified
- `FunLang/Cli.fs` - Argu argument type with CliArgs discriminated union
- `FunLang/FunLang.fsproj` - Added Argu 6.2.5 package, Cli.fs compile order
- `FunLang/Program.fs` - Argu-based CLI parsing with ProcessExiter error handling

## Decisions Made
- Used Argu's automatic underscore-to-hyphen conversion (Emit_Tokens becomes --emit-tokens)
- MainCommand attribute on File enables positional argument without flag
- ProcessExiter with colorizer for red error messages (but no color for help text)
- raiseOnUsage = false to print help without throwing exception

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - Argu integrated cleanly with existing CLI behavior.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- CLI infrastructure modernized and ready for REPL flag handling
- ArgumentParser in place for easy extension with new commands
- All 93 fslit tests and 168 Expecto tests passing

---
*Phase: 03-repl*
*Completed: 2026-01-31*
